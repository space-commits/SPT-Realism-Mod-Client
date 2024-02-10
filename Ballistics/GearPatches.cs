using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using System;
using System.Reflection;
using System.Linq;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Comfort.Common;
using static RealismMod.Attributes;
using UnityEngine;
using UnityEngine.UI;
using BPConstructor = GClass2680;
using BPTemplate = GClass2583;
using RigConstructor = GClass2681;
using RigTemplate = GClass2584; 
using HeadsetClass = GClass2635;
using HeadsetTemplate = GClass2538;
using ArmorCompTemplate = GInterface280;
using HarmonyLib;


namespace RealismMod
{
    public class EquipmentPenaltyComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EquipmentPenaltyComponent).GetConstructor(new Type[] { typeof(Item), typeof(GInterface282), typeof(bool) });
        }

        private static float getAverage(Func<CompositeArmorComponent, float> sumPredicate, Item item)
        {
            List<CompositeArmorComponent> listofComps = item.GetItemComponentsInChildren<CompositeArmorComponent>(true).ToList();
            if (!listofComps.Any()) 
            {
                return 0f;
            }
            return listofComps.Average(sumPredicate);
        }

        private static float getAverageBlunt(Item item)
        {
            return getAverage(new Func<CompositeArmorComponent, float>(getBluntThroughput), item);
        }

        private static float getBluntThroughput(CompositeArmorComponent subArmor)
        {
            return subArmor.BluntThroughput;
        }

        [PatchPostfix]
        private static void PatchPostfix(EquipmentPenaltyComponent __instance, Item item, bool anyArmorPlateSlots)
        {
            float comfortModifier = GearStats.ComfortModifier(item);
            if (comfortModifier > 0f && comfortModifier != 1f)
            {
                float comfortPercent = -1f * (float)Math.Round((comfortModifier - 1f) * 100f);
                List<ItemAttributeClass> comfortAtt = item.Attributes;
                ItemAttributeClass comfortAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.Comfort);
                comfortAttClass.Name = ENewItemAttributeId.Comfort.GetName();
                comfortAttClass.Base = () => comfortPercent;
                comfortAttClass.StringValue = () => comfortPercent.ToString() + " %";
                comfortAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                comfortAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                comfortAttClass.LessIsGood = false;
                comfortAtt.Add(comfortAttClass);
            }

            ArmorComponent armorComp; 
            if (anyArmorPlateSlots || item.TryGetItemComponent<ArmorComponent>(out armorComp)) 
            {
                List<ItemAttributeClass> bluntAtt = item.Attributes;
                ItemAttributeClass bluntAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.BluntThroughput);
                bluntAttClass.Name = ENewItemAttributeId.BluntThroughput.GetName();
                bluntAttClass.Base = () => 1f - getAverageBlunt(item) * 100f;
                bluntAttClass.StringValue = () => ((1f - getAverageBlunt(item)) * 100f).ToString() + " %";
                bluntAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                bluntAtt.Add(bluntAttClass);


                if (Plugin.ServerConfig.realistic_ballistics)
                {
                    bool canSpall = GearStats.CanSpall(__instance.Item);
                    if (canSpall)
                    {
                        List<ItemAttributeClass> canSpallAtt = item.Attributes;
                        ItemAttributeClass canSpallAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.CanSpall);
                        canSpallAttClass.Name = ENewItemAttributeId.CanSpall.GetName();
                        canSpallAttClass.StringValue = () => "";
                        canSpallAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        canSpallAtt.Add(canSpallAttClass);

                        List<ItemAttributeClass> spallReductAtt = item.Attributes;
                        ItemAttributeClass spallReductAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.SpallReduction);
                        spallReductAttClass.Name = ENewItemAttributeId.SpallReduction.GetName();
                        spallReductAttClass.StringValue = () => ((1 - GearStats.SpallReduction(item)) * 100f).ToString() + " %";
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
            if (template.ArmorType == EArmorType.None) 
            {
                Item item = __instance as Item;

                float gearReloadSpeed = GearStats.ReloadSpeedMulti(item);
                float comfortModifier = GearStats.ComfortModifier(item);

                if (gearReloadSpeed > 0f && gearReloadSpeed != 1f)
                {
                    float reloadSpeedPercent = (float)Math.Round((gearReloadSpeed - 1f) * 100f);
                    List<ItemAttributeClass> reloadAtt = item.Attributes;
                    ItemAttributeClass reloadAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.GearReloadSpeed);
                    reloadAttClass.Name = ENewItemAttributeId.GearReloadSpeed.GetName();
                    reloadAttClass.Base = () => reloadSpeedPercent;
                    reloadAttClass.StringValue = () => reloadSpeedPercent.ToString() + " %";
                    reloadAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    reloadAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                    reloadAttClass.LessIsGood = false;
                    reloadAtt.Add(reloadAttClass);
                }

                if (comfortModifier > 0f && comfortModifier != 1f)
                {
                    float comfortPercent = -1f * (float)Math.Round((comfortModifier - 1f) * 100f);
                    List<ItemAttributeClass> comfortAtt = item.Attributes;
                    ItemAttributeClass comfortAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.Comfort);
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

            float dB = GearStats.DbLevel(item);

            if (dB > 0)
            {
                List<ItemAttributeClass> dbAtt = item.Attributes;
                ItemAttributeClass dbAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.NoiseReduction);
                dbAttClass.Name = ENewItemAttributeId.NoiseReduction.GetName();
                dbAttClass.Base = () => dB;
                dbAttClass.StringValue = () => dB.ToString() + " NRR";
                dbAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                dbAtt.Add(dbAttClass);
            }
        }
    }
}
