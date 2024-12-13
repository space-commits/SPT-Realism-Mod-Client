using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static RealismMod.Attributes;
using ArmorPlateUIClass = GClass3485; // guess
using FormatArmorClass = GClass2534;
using StatAttributeClass = ItemAttributeClass;
using RootMotion.FinalIK;
using System.Xml.Linq;

namespace RealismMod
{
    public class ArmorClassStringPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ArmorComponent.Class2118).GetMethod("method_3", BindingFlags.Instance | BindingFlags.Public);
        }

        private static string GetItemClass(CompositeArmorComponent x)
        {
            return x.Item.ShortName.Localized(null) + ": " + GearStats.ArmorClass(x.Item);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ArmorComponent.Class2118 __instance, ref string __result)
        {
            CompositeArmorComponent[] array = __instance.item.GetItemComponentsInChildren<CompositeArmorComponent>(true).ToArray<CompositeArmorComponent>();

            if (array.Length > 1)
            {
                __result = array.Select(new Func<CompositeArmorComponent, string>(GetItemClass)).CastToStringValue("\n", true);
            }
            __result = GearStats.ArmorClass(__instance.armorComponent_0.Item);
            return false;
        }
    }


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
        private static void PatchPrefix(ItemViewStats __instance, ArmoredEquipmentItemClass armorPlate)
        {
            Image armorClassImage = (Image)AccessTools.Field(typeof(ItemViewStats), "_armorClassIcon").GetValue(__instance);
            if (armorPlate.Armor.Template.ArmorClass > 6)
            {
                string armorClass = string.Format("{0}.png", armorPlate.Armor.Template.ArmorClass);
                armorClassImage.sprite = Plugin.LoadedSprites[armorClass];
            }
        }
    }

    public class PenetrationUIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3485).GetConstructor(new Type[] { typeof(float) });
        }

        private static string GetStringValues(int armorClass, float penetrationPower)
        {
            float penetrationChance = GClass623.RealResistance(100f, 100f, armorClass, penetrationPower).GetPenetrationChance(penetrationPower);
            string armorClassString = armorClass >= 10 ? "Lvl " + armorClass + " " : "Lvl " + armorClass + " "; //string.Format("<sprite name=\"armor_classes_{0}\"> ", armorClass)
            return armorClassString + GClass3485.smethod_0(penetrationChance);
        }


        [PatchPostfix]
        private static void PatchPrefix(GClass3485 __instance, float penetrationPower)
        {
            List<GClass3114> list = new List<GClass3114>(); // not enough info to find this class currently
            for (int i = 1; i <= 10; i++)
            {
                list.Add(new GClass3114(GetStringValues(i, penetrationPower), null));
            }
            AccessTools.Field(typeof(GClass3485), "Lines").SetValue(__instance, list.ToArray());
        }
    }

    //ammo stats
    public class GetCachedReadonlyQualitiesPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("GetCachedReadonlyQualities", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix(AmmoTemplate __instance, ref List<ItemAttributeClass> __result)
        {

            if (!__result.Any((ItemAttributeClass i) => (ENewItemAttributeId)i.Id == ENewItemAttributeId.BallisticCoefficient) && !__result.Any((ItemAttributeClass i) => (ENewItemAttributeId)i.Id == ENewItemAttributeId.ProjectileCount))
            {
                AddCustomAttributes(__instance, ref __result);
            }

        }

        private static string GetHeat(AmmoTemplate __instance)
        {
            float heat = __instance.HeatFactor;
            switch (heat)
            {
                case <= 1f:
                    return "";
                case <= 1.05f:
                    return "Low";
                case <= 1.1f:
                    return "Significant";
                case <= 1.15f:
                    return "High";
                case <= 1.2f:
                    return "Very High";
                case > 1.2f:
                    return "Extreme";
            }
            return string.Empty;
        }


        private static string GetMalfChance(AmmoTemplate __instance)
        {
            float malfChance = __instance.MalfMisfireChance;
            switch (malfChance)
            {
                case <= 0f:
                    return "";
                case <= 0.5f:
                    return "Low Chance";
                case <= 1f:
                    return "Significant Chance";
                case <= 1.5f:
                    return "High Chance";
                case <= 2f:
                    return  "Very High Chance";
                case > 2f:
                    return "Extremely High Chance";
            }
            return string.Empty;
        }


        private static string GetDuraBurnString(AmmoTemplate __instance)
        {
            float duraBurn = __instance.DurabilityBurnModificator - 1f;
            switch (duraBurn)
            {
                case <= 1.25f:
                    return "Very Low";
                case <= 1.5f:
                    return  "Low";
                case <= 2f:
                    return "Significant";
                case <= 4f:
                    return "High";
                case <= 8f:
                    return "Very High";
                case > 8f:
                    return "Extreme";
            }
            return string.Empty;
        }

        public static void AddCustomAttributes(AmmoTemplate ammoTemplate, ref List<ItemAttributeClass> ammoAttributes)
        {
            if (PluginConfig.EnableAmmoStats.Value == true)
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

                if (ammoTemplate.BallisticCoeficient > 0f)
                {
                    ItemAttributeClass bcAtt = new ItemAttributeClass(ENewItemAttributeId.BallisticCoefficient);
                    bcAtt.Name = ENewItemAttributeId.BallisticCoefficient.GetName();
                    bcAtt.Base = () => ammoTemplate.BallisticCoeficient;
                    bcAtt.StringValue = () => $"{ammoTemplate.BallisticCoeficient}";
                    bcAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(bcAtt);
                }

                if (ammoTemplate.MalfMisfireChance > 0f)
                {
                    ItemAttributeClass malfAtt = new ItemAttributeClass(ENewItemAttributeId.MalfunctionChance);
                    malfAtt.Name = ENewItemAttributeId.MalfunctionChance.GetName();
                    malfAtt.Base = () => ammoTemplate.MalfMisfireChance;
                    malfAtt.StringValue = () => GetMalfChance(ammoTemplate);
                    malfAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(malfAtt);
                }

                if (ammoTemplate.DurabilityBurnModificator > 1f)
                {
                    ItemAttributeClass duraBurnAtt = new ItemAttributeClass(ENewItemAttributeId.DurabilityBurn);
                    duraBurnAtt.Name = ENewItemAttributeId.DurabilityBurn.GetName();
                    duraBurnAtt.Base = () => ammoTemplate.DurabilityBurnModificator;
                    duraBurnAtt.StringValue = () => GetDuraBurnString(ammoTemplate);
                    duraBurnAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(duraBurnAtt);
                }

                if (ammoTemplate.HeatFactor > 1f)
                {
                    ItemAttributeClass heatAtt = new ItemAttributeClass(ENewItemAttributeId.Heat);
                    heatAtt.Name = ENewItemAttributeId.Heat.GetName();
                    heatAtt.Base = () => ammoTemplate.HeatFactor;
                    heatAtt.StringValue = () => GetHeat(ammoTemplate);
                    heatAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(heatAtt);
                }
            }
        }
    }

    public class MagazineMalfChanceDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MagazineItemClass).GetMethod("method_40", BindingFlags.Instance | BindingFlags.Public);
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
        private static bool Prefix(MagazineItemClass __instance, ref string __result)
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

    public class ModVRecoilStatDisplayPatchFloat : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_15", BindingFlags.Instance | BindingFlags.Public);
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
            return typeof(Mod).GetMethod("method_16", BindingFlags.Instance | BindingFlags.Public);
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
            return typeof(Mod).GetMethod("method_18", BindingFlags.Instance | BindingFlags.Public);
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

        private static string[] _multiCals = new string[] { "556x45NATO", "762x35", "762x51", "762x39", "366TKM" };

        private static string GetCaliber(IEnumerable<Mod> mods)
        {
            int count = mods.Count();
            foreach (var mod in mods)
            {
                string modType = AttachmentProperties.ModType(mod);
                bool isBarrel = Utils.IsBarrel(mod);
                if (isBarrel && _multiCals.Contains(modType))
                {
                    return modType;
                }
            }
            return null;
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {

            if (Utils.PlayerIsReady && __instance?.Owner?.ID != Singleton<GameWorld>.Instance?.MainPlayer?.ProfileId) return true;
            string cal = GetCaliber(__instance.Mods);
            if (cal == null) return true;
            __result = cal;
            return false;
        }
    }

    public class BarrelModClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BarrelItemClass).GetConstructor(new Type[] { typeof(string), typeof(BarrelTemplateClass) });
        }

        [PatchPostfix]
        private static void PatchPostfix(BarrelItemClass __instance, BarrelTemplateClass template)
        {
            float shotDisp = (template.ShotgunDispersion - 1f) * 100f;

            ItemAttributeClass shotDispAtt = new ItemAttributeClass(ENewItemAttributeId.ShotDispersion);
            shotDispAtt.Name = ENewItemAttributeId.ShotDispersion.GetName();
            shotDispAtt.Base = () => shotDisp;
            shotDispAtt.StringValue = () => $"{shotDisp}%";
            shotDispAtt.LessIsGood = true;
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
            float muzzleFlash = AttachmentProperties.ModFlashSuppression(__instance);
            float aimStability = AttachmentProperties.ModAimStability(__instance);
            float handling = AttachmentProperties.ModHandling(__instance);

            if (Plugin.ServerConfig.malf_changes == true)
            {
                Utils.AddAttribute(__instance, ENewItemAttributeId.MalfunctionChance, malfChance, $"{getMalfOdds(malfChance)}", true);
            }

            if (AttachmentProperties.StockAllowADS(__instance))
            {
                Utils.AddAttribute(__instance, ENewItemAttributeId.CanADS, 1, "", colored: false);
            }

            if (muzzleFlash != 0f && PluginConfig.EnableMuzzleEffects.Value)
            {
                bool isGasReduction = !Utils.IsMuzzleCombo(__instance) && !Utils.IsFlashHider(__instance) && !Utils.IsBarrel(__instance);
                float flashValue = muzzleFlash * (isGasReduction ? 1f : -1f);
                string name = isGasReduction ? ENewItemAttributeId.Gas.GetName() : ENewItemAttributeId.MuzzleFlash.GetName();
                Utils.AddAttribute(__instance, ENewItemAttributeId.MuzzleFlash, flashValue, $"{flashValue}%", isGasReduction ? true : false, name);
            }

            Utils.AddAttribute(__instance, ENewItemAttributeId.HorizontalRecoil, hRecoil, $"{hRecoil}%", true);
            Utils.AddAttribute(__instance, ENewItemAttributeId.VerticalRecoil, vRecoil, $"{vRecoil}%", true);
            Utils.AddAttribute(__instance, ENewItemAttributeId.Dispersion, disperion, $"{disperion}%", true);
            Utils.AddAttribute(__instance, ENewItemAttributeId.CameraRecoil, cameraRecoil, $"{cameraRecoil}%", true);
            Utils.AddAttribute(__instance, ENewItemAttributeId.AutoROF, autoROF, $"{autoROF}%", true);
            Utils.AddAttribute(__instance, ENewItemAttributeId.SemiROF, semiROF, $"{semiROF}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.RecoilAngle, angle, $"{angle}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.ReloadSpeed, reloadSpeed, $"{reloadSpeed}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.ChamberSpeed, chamberSpeed, $"{chamberSpeed}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.AimSpeed, aimSpeed, $"{aimSpeed}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.AimStability, aimStability, $"{aimStability}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.Handling, handling, $"{handling}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.ShotDispersion, shotDisp, $"{shotDisp}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.Convergence, conv, $"{conv}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.MeleeDamage, meleeDmg, $"{meleeDmg}%", false);
            Utils.AddAttribute(__instance, ENewItemAttributeId.MeleePen, meleePen, $"{meleePen}%", false);
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
            return typeof(BarrelItemClass).GetMethod("get_CenterOfImpactMOA", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(BarrelItemClass __instance, ref float __result)
        {
            BarrelComponent itemComponent = __instance.GetItemComponent<BarrelComponent>();
            if (itemComponent == null) __result = 0f;
            else __result = (float)Math.Round((double)(100f * itemComponent.Template.CenterOfImpact / 2.9089f) * 2, 2);

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

            if (PluginConfig.ShowBalance.Value == true)
            {
                List<ItemAttributeClass> balanceAttList = __instance.Attributes;
                StatAttributeClass balanceAtt = new StatAttributeClass((EItemAttributeId)ENewItemAttributeId.Balance);
                balanceAtt.Name = ENewItemAttributeId.Balance.GetName();
                //balanceAtt.Range = new Vector2(100f, 200f); // ItemAttributeClass is the only one with LessIsGood as a value, but its missing this
                balanceAtt.LessIsGood = false;
                balanceAtt.Base = () => 150;
                //balanceAtt.Delta = () => BalanceDelta();
                balanceAtt.StringValue = () => Math.Round(UIWeaponStats.Balance, 1).ToString();
                balanceAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                balanceAttList.Add(balanceAtt);

            }

            if (PluginConfig.ShowDispersion.Value == true)
            {
                List<ItemAttributeClass> dispersionAttList = __instance.Attributes;
                StatAttributeClass dispersionAtt = new StatAttributeClass((EItemAttributeId)ENewItemAttributeId.Dispersion);
                dispersionAtt.Name = ENewItemAttributeId.Dispersion.GetName();
                //dispersionAtt.Range = new Vector2(0f, 50f);
                dispersionAtt.LessIsGood = true;
                dispersionAtt.Base = () => __instance.Template.RecolDispersion;
                //dispersionAtt.Delta = () => DispersionDelta(__instance);
                dispersionAtt.StringValue = () => Math.Round(UIWeaponStats.Dispersion, 1).ToString();
                dispersionAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                dispersionAttList.Add(dispersionAtt);
            }

            if (PluginConfig.ShowCamRecoil.Value == true)
            {
                List<ItemAttributeClass> camRecoilAttList = __instance.Attributes;
                StatAttributeClass camRecoilAtt = new StatAttributeClass((EItemAttributeId)ENewItemAttributeId.CameraRecoil);
                camRecoilAtt.Name = ENewItemAttributeId.CameraRecoil.GetName();
                //camRecoilAtt.Range = new Vector2(0f, 50f);
                camRecoilAtt.LessIsGood = true;
                camRecoilAtt.Base = () => __instance.Template.RecoilCamera * 100f;
                //camRecoilAtt.Delta = () => CamRecoilDelta(__instance);
                camRecoilAtt.StringValue = () => Math.Round(UIWeaponStats.CamRecoil * 100f, 2).ToString();
                camRecoilAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                camRecoilAttList.Add(camRecoilAtt);
            }

            if (PluginConfig.ShowRecoilAngle.Value == true)
            {
                List<ItemAttributeClass> recoilAngleAttList = __instance.Attributes;
                ItemAttributeClass recoilAngleAtt = new ItemAttributeClass(ENewItemAttributeId.RecoilAngle);
                recoilAngleAtt.Name = ENewItemAttributeId.RecoilAngle.GetName();
                recoilAngleAtt.Base = () => UIWeaponStats.RecoilAngle;
                recoilAngleAtt.StringValue = () => Math.Round(UIWeaponStats.RecoilAngle).ToString();
                recoilAngleAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                recoilAngleAttList.Add(recoilAngleAtt);
            }

            if (PluginConfig.ShowSemiROF.Value == true)
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
            return typeof(Weapon).GetMethod("method_25", BindingFlags.Instance | BindingFlags.Public);
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
            return typeof(Weapon).GetMethod("method_26", BindingFlags.Instance | BindingFlags.Public);
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
            return typeof(Weapon).GetMethod("method_22", BindingFlags.Instance | BindingFlags.Public);
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
            return typeof(Weapon).GetMethod("method_23", BindingFlags.Instance | BindingFlags.Public);
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
            return typeof(Weapon).GetMethod("method_17", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            float durability = __instance.method_9((float)__instance.Repairable.TemplateDurability);
            float durabilityFactor = (__instance.GetBarrelDeviation() - durability) / (__instance.Single_0 - durability);
            __result = UIWeaponStats.COIDelta + durabilityFactor;
            return false;
        }
    }

    public class COIDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_18", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            __result = ((GetTotalCOI(__instance, true) * __instance.GetBarrelDeviation() * 100f / 2.9089f) * 2f).ToString("0.0#") + " " + "moa".Localized(null);
            return false;
        }

        private static float GetTotalCOI(Weapon __instance, bool includeAmmo)
        {
            float totalCOI = __instance.CenterOfImpactBase * (1f + (UIWeaponStats.COIDelta));
            if (!includeAmmo)
            {
                return totalCOI;
            }
            AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;

            return totalCOI * ((currentAmmoTemplate != null) ? currentAmmoTemplate.AmmoFactor : 1f);
        }
    }

    public class FireRateDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_35", BindingFlags.Instance | BindingFlags.Public);
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
            return typeof(Weapon).GetMethod("method_14", BindingFlags.Instance | BindingFlags.Public);
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
            return typeof(Weapon).GetMethod("method_15", BindingFlags.Instance | BindingFlags.Public);
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
            bool isChonker = __instance.IsBeltMachineGun || __instance.Weight > 10f;

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
            float currentShotDisp = 0f;
            float currentMalfChance = 0f;

            string weapOpType = WeaponStats.OperationType(__instance);
            string weapType = WeaponStats.WeaponType(__instance);

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
                    modWeight = mod.Weight;
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
                float modMalfChance = 0;
                float modDuraBurn = 0;
                float modChamber = 0;
                float modFlashSuppression = 0;
                float modStability = 0;
                float modHandling = 0;
                float modLoudness = 0;
                float modAim = 0;

                string modType = AttachmentProperties.ModType(mod);
                string position = StatCalc.GetModPosition(mod, weapType, weapOpType, modType);
                StatCalc.ModConditionalStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, 
                    ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, 
                    ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position, ref modChamber, ref modLoudness, 
                    ref modMalfChance, ref modDuraBurn, ref modConv, ref modFlashSuppression, ref modStability, 
                    ref modHandling, ref modAim);

                StatCalc.ModStatCalc(
                    mod, true, isChonker, modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, 
                    ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, 
                    modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, 
                    ref currentCOI, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, 
                    ref currentHRecoil, ref pureErgo, 0, ref currentShotDisp, ref currentMalfChance, 
                    0, ref pureRecoil, ref currentConv, modConv, ref currentCamReturnSpeed);
            }


            float ergoLessMag = 0;
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

            StatCalc.WeaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref totalRecoilDamping, ref totalRecoilHandDamping, currentCOI, hasShoulderContact, ref totalCOI, ref totalCOIDelta, baseCOI, pureErgo, ref pureErgoDelta, ref ergoLessMag, 0f, true);

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
