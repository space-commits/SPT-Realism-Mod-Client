using EFT.CameraControl;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static RealismMod.Attributes;
using HeadsetClass = HeadphonesItemClass; //updatephonesreally()
using HeadsetTemplate = HeadphonesTemplateClass; //updatephonesreally()
using IEquipmentPenalty = GInterface346;
using RigConstructor = VestItemClass;
using RigTemplate = VestTemplateClass; //the one without the blindness stat


namespace RealismMod
{

    public class FaceshieldMaskPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerCameraController).GetMethod("method_3", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(PlayerCameraController __instance, FaceShieldComponent faceShield)
        {
            if (faceShield != null && faceShield?.Item != null && Plugin.LoadedTextures.Count > 0)
            {
                VisorEffect visor = (VisorEffect)AccessTools.Field(typeof(PlayerCameraController), "visorEffect_0").GetValue(__instance);
                Material mat = visor.method_4();

                var gearStats = Stats.GetDataObj<Gear>(Stats.GearStats, faceShield.Item.TemplateId);
                string maskToUse = gearStats.MaskToUse;
                if (maskToUse == null || maskToUse == string.Empty || maskToUse == "") return;
                Texture mask = Plugin.LoadedTextures[maskToUse + ".png"];
                mat.SetTexture("_Mask", mask);
            }
        }
    }


    public class EquipmentPenaltyComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EquipmentPenaltyComponent).GetConstructor(new Type[] { typeof(Item), typeof(IEquipmentPenalty), typeof(bool) });
        }

        private static float GetAverage(Func<CompositeArmorComponent, float> predicate, Item item)
        {
            List<CompositeArmorComponent> listofComps = item.GetItemComponentsInChildren<CompositeArmorComponent>(true).ToList();
            if (!listofComps.Any())
            {
                return 0f;
            }
            return (float)Math.Round(listofComps.Average(predicate), 2);
        }

        private static float GetAverageBlunt(Item item)
        {
            return GetAverage(new Func<CompositeArmorComponent, float>(GetBluntThroughput), item);
        }

        private static float GetBluntThroughput(CompositeArmorComponent subArmor)
        {
            return subArmor.BluntThroughput;
        }

        [PatchPostfix]
        private static void PatchPostfix(EquipmentPenaltyComponent __instance, Item item, bool anyArmorPlateSlots)
        {
            var gearStats = Stats.GetDataObj<Gear>(Stats.GearStats, item.TemplateId);
            if (Plugin.ServerConfig.gear_weight && (anyArmorPlateSlots || item.Template.ParentId == "5448e53e4bdc2d60728b4567"))
            {
                        float comfortModifier = gearStats.Comfort;
                if (comfortModifier > 0f && comfortModifier != 1f)
                {
                    float comfortPercent = -1f * (float)Math.Round((comfortModifier - 1f) * 100f);
                    List<ItemAttributeClass> comfortAtt = item.Attributes;
                    ItemAttributeClass comfortAttClass = new ItemAttributeClass(ENewItemAttributeId.Comfort);
                    comfortAttClass.Name = ENewItemAttributeId.Comfort.GetName();
                    comfortAttClass.Base = () => comfortPercent;
                    comfortAttClass.StringValue = () => comfortPercent.ToString() + " %";
                    comfortAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    comfortAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                    comfortAttClass.LessIsGood = false;
                    comfortAtt.Add(comfortAttClass);
                }
            }

            float gasProtection = gearStats.GasProtection;
            if (gasProtection > 0f) 
            {
                gasProtection = gasProtection * 100f;
                List<ItemAttributeClass> gasAtt = item.Attributes;
                ItemAttributeClass gasAttClass = new ItemAttributeClass(ENewItemAttributeId.GasProtection);
                gasAttClass.Name = ENewItemAttributeId.GasProtection.GetName();
                gasAttClass.Base = () => gasProtection;
                gasAttClass.StringValue = () => gasProtection.ToString() + " %";
                gasAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                gasAtt.Add(gasAttClass);
            }

            float radProtection = gearStats.RadProtection;
            if (radProtection > 0f)
            {
                radProtection = radProtection * 100f;
                List<ItemAttributeClass> radAtt = item.Attributes;
                ItemAttributeClass radAttClass = new ItemAttributeClass(ENewItemAttributeId.RadProtection);
                radAttClass.Name = ENewItemAttributeId.RadProtection.GetName();
                radAttClass.Base = () => radProtection;
                radAttClass.StringValue = () => radProtection.ToString() + " %";
                radAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                radAtt.Add(radAttClass);
            }

            bool allowADS = gearStats.AllowADS;
            if (!allowADS)
            {
                List<ItemAttributeClass> canADSAtt = __instance.Item.Attributes;
                ItemAttributeClass canADSAttAttClass = new ItemAttributeClass(ENewItemAttributeId.CantADS);
                canADSAttAttClass.Name = ENewItemAttributeId.CantADS.GetName();
                canADSAttAttClass.StringValue = () => "";
                canADSAttAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                canADSAtt.Add(canADSAttAttClass);
            }

            ArmorComponent armorComp;
            if (anyArmorPlateSlots || item.TryGetItemComponent(out armorComp))
            {
                List<ItemAttributeClass> bluntAtt = item.Attributes;
                ItemAttributeClass bluntAttClass = new ItemAttributeClass(ENewItemAttributeId.BluntThroughput);
                bluntAttClass.Name = ENewItemAttributeId.BluntThroughput.GetName();
                bluntAttClass.Base = () => 1f - GetAverageBlunt(item) * 100f;
                bluntAttClass.StringValue = () => ((1f - GetAverageBlunt(item)) * 100f).ToString() + " %";
                bluntAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                bluntAtt.Add(bluntAttClass);


                if (Plugin.ServerConfig.realistic_ballistics)
                {
                    bool canSpall = gearStats.CanSpall;
                    if (canSpall)
                    {
                        List<ItemAttributeClass> canSpallAtt = item.Attributes;
                        ItemAttributeClass canSpallAttClass = new ItemAttributeClass(ENewItemAttributeId.CanSpall);
                        canSpallAttClass.Name = ENewItemAttributeId.CanSpall.GetName();
                        canSpallAttClass.StringValue = () => "";
                        canSpallAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        canSpallAtt.Add(canSpallAttClass);

                        List<ItemAttributeClass> spallReductAtt = item.Attributes;
                        ItemAttributeClass spallReductAttClass = new ItemAttributeClass(ENewItemAttributeId.SpallReduction);
                        spallReductAttClass.Name = ENewItemAttributeId.SpallReduction.GetName();
                        spallReductAttClass.StringValue = () => ((1 - gearStats.SpallReduction) * 100f).ToString() + " %";
                        spallReductAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        spallReductAtt.Add(spallReductAttClass);
                    }
                }
            }
        }
    }

    public class RigConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RigConstructor).GetConstructor(new Type[] { typeof(string), typeof(RigTemplate) });
        }


        [PatchPostfix]
        private static void PatchPostfix(RigConstructor __instance, RigTemplate template)
        {
            Item item = __instance as Item;
            var gearStats = Stats.GetDataObj<Gear>(Stats.GearStats, item.TemplateId);
            if (Plugin.ServerConfig.reload_changes)
            {
                float gearReloadSpeed = gearStats.ReloadSpeedMulti;
                if (gearReloadSpeed > 0f && gearReloadSpeed != 1f)
                {
                    float reloadSpeedPercent = (float)Math.Round((gearReloadSpeed - 1f) * 100f);
                    List<ItemAttributeClass> reloadAtt = item.Attributes;
                    ItemAttributeClass reloadAttClass = new ItemAttributeClass(ENewItemAttributeId.GearReloadSpeed);
                    reloadAttClass.Name = ENewItemAttributeId.GearReloadSpeed.GetName();
                    reloadAttClass.Base = () => reloadSpeedPercent;
                    reloadAttClass.StringValue = () => reloadSpeedPercent.ToString() + " %";
                    reloadAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    reloadAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                    reloadAttClass.LessIsGood = false;
                    reloadAtt.Add(reloadAttClass);
                }
            }

            if (Plugin.ServerConfig.gear_weight)
            {
                if (template.ArmorType == EArmorType.None)
                {
                    float comfortModifier = gearStats.Comfort;
                    if (comfortModifier > 0f && comfortModifier != 1f)
                    {
                        float comfortPercent = -1f * (float)Math.Round((comfortModifier - 1f) * 100f);
                        List<ItemAttributeClass> comfortAtt = item.Attributes;
                        ItemAttributeClass comfortAttClass = new ItemAttributeClass(ENewItemAttributeId.Comfort);
                        comfortAttClass.Name = ENewItemAttributeId.Comfort.GetName();
                        comfortAttClass.Base = () => comfortPercent;
                        comfortAttClass.StringValue = () => comfortPercent.ToString() + " %";
                        comfortAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        comfortAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                        comfortAttClass.LessIsGood = false;
                        comfortAtt.Add(comfortAttClass);
                    }
                }
            }
        }
    }

    public class HeadsetConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HeadsetClass).GetConstructor(new Type[] { typeof(string), typeof(HeadsetTemplate) });
        }


        [PatchPostfix]
        private static void PatchPostfix(HeadsetClass __instance)
        {
            Item item = __instance as Item;
            var gearStats = Stats.GetDataObj<Gear>(Stats.GearStats, item.TemplateId);
            float dB = gearStats.dB;

            if (dB > 0)
            {
                List<ItemAttributeClass> dbAtt = item.Attributes;
                ItemAttributeClass dbAttClass = new ItemAttributeClass(ENewItemAttributeId.NoiseReduction);
                dbAttClass.Name = ENewItemAttributeId.NoiseReduction.GetName();
                dbAttClass.Base = () => dB;
                dbAttClass.StringValue = () => dB.ToString() + " NRR";
                dbAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                dbAtt.Add(dbAttClass);
            }
        }
    }
}
