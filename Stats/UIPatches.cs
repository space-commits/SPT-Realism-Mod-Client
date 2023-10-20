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
using StatAttributeClass = GClass2563;
using BarrelTemplateClass = GClass2394;

namespace RealismMod
{
    public class MountingUI : MonoBehaviour
    {
        public static GameObject ActiveUIScreen;
        private static GameObject mountingUIGameObject;
        private static Image mountingUIImage;
        private static RectTransform mountingUIRect;

        public static void DestroyGameObjects()
        {
            if (mountingUIGameObject != null)
            {
                Destroy(mountingUIGameObject);
            }
        }

        public static void CreateGameObjects(UnityEngine.Transform parent)
        {
            mountingUIGameObject = new GameObject("MountingUI");
            mountingUIRect = mountingUIGameObject.AddComponent<RectTransform>();
            mountingUIRect.anchoredPosition = new Vector2(100, 100);
            mountingUIImage = mountingUIGameObject.AddComponent<Image>();
            mountingUIGameObject.transform.SetParent(parent);
            mountingUIImage.sprite = Plugin.LoadedSprites["mounting.png"];
            mountingUIImage.raycastTarget = false;
            mountingUIImage.color = Color.clear;
            mountingUIRect.sizeDelta = new Vector2(100, 100);
        }

        public void Update() 
        {
            if (ActiveUIScreen != null && Plugin.EnableMountUI.Value)
            {
                if (StanceController.IsBracingLeftSide)
                {
                    mountingUIImage.sprite = Plugin.LoadedSprites["mountingleft.png"];
                }
                else if (StanceController.IsBracingRightSide)
                {
                    mountingUIImage.sprite = Plugin.LoadedSprites["mountingright.png"];
                }
                else 
                {
                    mountingUIImage.sprite = Plugin.LoadedSprites["mounting.png"];
                }

                if (StanceController.IsMounting)
                {
                    mountingUIImage.color = Color.white;
                    float scaleAmount = Mathf.Lerp(1f, 1.15f, Mathf.PingPong(Time.time * 0.9f, 1f));
                    mountingUIRect.sizeDelta = new Vector2(90f, 90f) * scaleAmount;

                }
                else if (StanceController.IsBracing) 
                {
                    mountingUIRect.sizeDelta = new Vector2(90f, 90f);
                    float alpha = Mathf.Lerp(0.2f, 1f, Mathf.PingPong(Time.time * 1f, 1f));
                    Color lerpedColor = new Color(1f, 1f, 1f, alpha);
                    mountingUIImage.color = lerpedColor;
                }
                else
                {
                    mountingUIImage.color = Color.clear;
                }
                mountingUIRect.localPosition = new Vector3(650f, -460f, 0f);
            }
        }
    }

    public class BattleUIScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.UI.BattleUIScreen).GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(EFT.UI.BattleUIScreen __instance)
        {
            if (MountingUI.ActiveUIScreen == __instance.gameObject) 
            {
                return;
            }
            MountingUI.ActiveUIScreen = __instance.gameObject;
            MountingUI.DestroyGameObjects();
            MountingUI.CreateGameObjects(__instance.transform);
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
            return typeof(AmmoTemplate).GetMethod("method_12", BindingFlags.Instance | BindingFlags.NonPublic);
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
            return typeof(MagazineClass).GetMethod("method_39", BindingFlags.Instance | BindingFlags.NonPublic);
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
            return typeof(AmmoTemplate).GetMethod("method_16", BindingFlags.Instance | BindingFlags.NonPublic);
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
            return typeof(Mod).GetMethod("method_15", BindingFlags.Instance | BindingFlags.NonPublic);
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
            return typeof(Mod).GetMethod("method_16", BindingFlags.Instance | BindingFlags.NonPublic);
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
            return typeof(Mod).GetMethod("method_18", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(Mod __instance, ref string __result)
        {
            __result = __instance.Ergonomics + "%";
            return false;
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

            if (Plugin.EnableMalfPatch.Value == true && Plugin.ModConfig.malf_changes == true)
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
                balanceAtt.StringValue = () => Math.Round(DisplayWeaponProperties.Balance, 1).ToString();
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
                dispersionAtt.StringValue = () => Math.Round(DisplayWeaponProperties.Dispersion, 1).ToString();
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
                camRecoilAtt.Base = () => __instance.Template.CameraRecoil * 100f;
                camRecoilAtt.Delta = () => CamRecoilDelta(__instance);
                camRecoilAtt.StringValue = () => Math.Round(DisplayWeaponProperties.CamRecoil * 100f, 2).ToString();
                camRecoilAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                camRecoilAttList.Add(camRecoilAtt);
            }

            if (Plugin.ShowRecoilAngle.Value == true)
            {
                List<ItemAttributeClass> recoilAngleAttList = __instance.Attributes;
                ItemAttributeClass recoilAngleAtt = new ItemAttributeClass(ENewItemAttributeId.RecoilAngle);
                recoilAngleAtt.Name = ENewItemAttributeId.RecoilAngle.GetName();
                recoilAngleAtt.Base = () => DisplayWeaponProperties.RecoilAngle;
                recoilAngleAtt.StringValue = () => Math.Round(DisplayWeaponProperties.RecoilAngle).ToString();
                recoilAngleAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                recoilAngleAttList.Add(recoilAngleAtt);
            }

            if (Plugin.ShowSemiROF.Value == true)
            {
                List<ItemAttributeClass> semiROFAttList = __instance.Attributes;
                ItemAttributeClass semiROFAtt = new ItemAttributeClass(ENewItemAttributeId.SemiROF);
                semiROFAtt.Name = ENewItemAttributeId.SemiROF.GetName();
                semiROFAtt.Base = () => DisplayWeaponProperties.SemiFireRate;
                semiROFAtt.StringValue = () => DisplayWeaponProperties.SemiFireRate.ToString() + " " + "RPM".Localized(null);
                semiROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                semiROFAttList.Add(semiROFAtt);
            }

        }

        private static float BalanceDelta()
        {
            float currentBalance = 150f - (DisplayWeaponProperties.Balance * -1f);
            return (150f - currentBalance) / (150f * -1f);
        }

        private static float DispersionDelta(Weapon __instance)
        {
            return (__instance.Template.RecolDispersion - DisplayWeaponProperties.Dispersion) / (__instance.Template.RecolDispersion * -1f);
        }

        private static float CamRecoilDelta(Weapon __instance)
        {
            float tempalteCam = __instance.Template.CameraRecoil * 100f;
            return (tempalteCam - (DisplayWeaponProperties.CamRecoil * 100f)) / (tempalteCam * -1f);
        }

    }

    public class HRecoilDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_25", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            __result = DisplayWeaponProperties.HRecoilDelta;
            return false;
        }
    }

    public class HRecoilDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_26", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            __result = Math.Round(__instance.Template.RecoilForceBack + __instance.Template.RecoilForceBack * DisplayWeaponProperties.HRecoilDelta, 1).ToString();
            return false;
        }
    }

    public class VRecoilDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_22", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            __result = DisplayWeaponProperties.VRecoilDelta - DisplayWeaponProperties.ConvergenceDelta;
            return false;
        }
    }

    public class VRecoilDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_23", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            __result = Math.Round(__instance.Template.RecoilForceUp + (__instance.Template.RecoilForceUp * (DisplayWeaponProperties.VRecoilDelta - DisplayWeaponProperties.ConvergenceDelta)), 1).ToString();
            return false;
        }

    }

    public class COIDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_17", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            float Single_0 = (float)AccessTools.Property(typeof(Weapon), "Single_0").GetValue(__instance);
            MethodInfo method_9 = AccessTools.Method(typeof(Weapon), "method_9");

            float num = (float)method_9.Invoke(__instance, new object[] { __instance.Repairable.TemplateDurability });
            float num2 = (__instance.GetBarrelDeviation() - num) / (Single_0 - num);
            __result = DisplayWeaponProperties.COIDelta + num2;
            return false;
        }
    }

    public class COIDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_18", BindingFlags.Instance | BindingFlags.NonPublic);
        }
         
        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            __result = ((GetTotalCOI(__instance, true) * __instance.GetBarrelDeviation() * 100f / 2.9089f) * 2f).ToString("0.0#") + " " + "moa".Localized(null);
            return false;
        }

        private static float GetTotalCOI(Weapon __instance, bool includeAmmo)
        {
            float num = __instance.CenterOfImpactBase * (1f + DisplayWeaponProperties.COIDelta);
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
            return typeof(Weapon).GetMethod("method_35", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(ref string __result)
        {
            __result = DisplayWeaponProperties.AutoFireRate.ToString() + " " + "RPM".Localized(null);
            return false;
        }
    }

    public class ErgoDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_14", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            if (Plugin.EnableStatsDelta.Value == true)
            {
                StatDeltaDisplay.DisplayDelta(__instance, Logger);
            }

            __result = DisplayWeaponProperties.ErgoDelta;
            return false;
        }
    }

    public class ErgoDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_15", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            StatDeltaDisplay.DisplayDelta(__instance, Logger);
            float ergoTotal = __instance.Template.Ergonomics * (1f + DisplayWeaponProperties.ErgoDelta);
            __result = Mathf.Clamp(ergoTotal, 0f, 100f).ToString("0.##");
            return false;
        }
    }

    public static class StatDeltaDisplay
    {
        public static void DisplayDelta(Weapon __instance, ManualLogSource logger)
        {
            float baseCOI = __instance.CenterOfImpactBase;
            float currentCOI = baseCOI;

            float baseAutoROF = __instance.Template.bFirerate;
            float currentAutoROF = baseAutoROF;

            float baseSemiROF = Mathf.Max(__instance.Template.SingleFireRate, 240);
            float currentSemiROF = baseSemiROF;

            float baseCamReturnSpeed = WeaponProperties.CameraReturnSpeed(__instance);
            float currentCamReturnSpeed = baseCamReturnSpeed;

            float baseCamRecoil = __instance.Template.CameraRecoil;
            float currentCamRecoil = baseCamRecoil;

            float baseConv = __instance.Template.Convergence;
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

            string weapOpType = WeaponProperties.OperationType(__instance);
            string weapType = WeaponProperties.WeaponType(__instance);

            float currentLoudness = 0;

            bool folded = __instance.Folded;

            bool hasShoulderContact = false;

            bool stockAllowsFSADS = false;


            if (WeaponProperties.WepHasShoulderContact(__instance) && !folded)
            {
                hasShoulderContact = true;
            }

            for (int i = 0; i < __instance.Mods.Length; i++)
            {
                Mod mod = __instance.Mods[i];
                float modWeight = __instance.Mods[i].Weight;
                if (Utils.IsMagazine(__instance.Mods[i]))
                {
                    modWeight = __instance.Mods[i].GetSingleItemTotalWeight();
                }
                float modWeightFactored = StatCalc.FactoredWeight(modWeight);
                float modErgo = __instance.Mods[i].Ergonomics;
                float modVRecoil = AttachmentProperties.VerticalRecoil(__instance.Mods[i]);
                float modHRecoil = AttachmentProperties.HorizontalRecoil(__instance.Mods[i]);
                float modAutoROF = AttachmentProperties.AutoROF(__instance.Mods[i]);
                float modSemiROF = AttachmentProperties.SemiROF(__instance.Mods[i]);
                float modCamRecoil = AttachmentProperties.CameraRecoil(__instance.Mods[i]);
                float modConv = AttachmentProperties.ModConvergence(__instance.Mods[i]);
                float modDispersion = AttachmentProperties.Dispersion(__instance.Mods[i]);
                float modAngle = AttachmentProperties.RecoilAngle(__instance.Mods[i]);
                float modAccuracy = __instance.Mods[i].Accuracy;
                float modReload = 0f;
                float modChamber = 0f;
                float modAim = 0f;
                float modLoudness = 0f;
                float modMalfChance = 0f;
                float modDuraBurn = 0f;
                float modFix = 0f;
                string modType = AttachmentProperties.ModType(__instance.Mods[i]);
                string position = StatCalc.GetModPosition(__instance.Mods[i], weapType, weapOpType, modType);

                StatCalc.ModConditionalStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position, ref modChamber, ref modLoudness, ref modMalfChance, ref modDuraBurn, ref modConv);
                StatCalc.ModStatCalc(mod, modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeed, modReload, ref currentReloadSpeed, modFix, ref currentFixSpeed, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil, ref currentChamberSpeed, modChamber, true, __instance.WeapClass, ref pureErgo, 0, ref currentShotDisp, modLoudness, ref currentLoudness, ref currentMalfChance, modMalfChance, ref pureRecoil, ref currentConv, modConv, ref currentCamReturnSpeed);
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

            /*     float ergoWeight = StatCalc.ErgoWeightCalc(__instance.GetSingleItemTotalWeight(), pureErgoDelta, totalTorque, __instance.WeapClass);
                 float ergoDisp = 80f - ergoWeight;
                 float ergoDispDelta = ergoWeight / -80f;*/

            DisplayWeaponProperties.TotalConvergence = currentConv;
            DisplayWeaponProperties.ConvergenceDelta = (currentConv - baseConv) / baseConv; 
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
            DisplayWeaponProperties.COIDelta = totalCOIDelta;
            DisplayWeaponProperties.CamReturnSpeed = currentCamReturnSpeed;
        }
    }
}
