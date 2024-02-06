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
using Diz.LanguageExtensions;

namespace RealismMod
{

    public class BackpackConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BPConstructor).GetConstructor(new Type[] { typeof(string), typeof(BPTemplate) });
        }

        [PatchPostfix]
        private static void PatchPostfix(BPConstructor __instance)
        {
            Item item = __instance as Item;

            float comfortModifier = GearStats.ComfortModifier(item);
            float comfortPercent = -1f * (float)Math.Round((comfortModifier - 1f) * 100f);

            if (comfortModifier > 0f && comfortModifier != 1f)
            {
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


    public class RigConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RigConstructor).GetConstructor(new Type[] { typeof(string), typeof(RigTemplate) });
        }


        [PatchPostfix]
        private static void PatchPostfix(RigConstructor __instance)
        {
            Item item = __instance as Item;

            float gearReloadSpeed = GearStats.ReloadSpeedMulti(item);
            float reloadSpeedPercent = (float)Math.Round((gearReloadSpeed - 1f) * 100f);

            float comfortModifier = GearStats.ComfortModifier(item);
            float comfortPercent = -1f * (float)Math.Round((comfortModifier - 1f) * 100f);

            if (gearReloadSpeed > 0f && gearReloadSpeed != 1f)
            {
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

    public class ArmorLevelDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2516).GetMethod("FormatArmorClassIcon", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(GClass2516 __instance, ref string __result, int armorClass)
        {
            Logger.LogWarning("FormatArmorClassIcon");
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
        private static void PatchPrefix(ItemViewStats __instance, GClass2629 armorPlate)
        {
            Image armorClassImage = (Image)AccessTools.Field(typeof(ItemViewStats), "_armorClassIcon").GetValue(__instance);
            if (armorPlate.Armor.Template.ArmorClass > 6) 
            {
                string armorClass = string.Format("{0}.png", armorPlate.Armor.Template.ArmorClass);
                armorClassImage.sprite = Plugin.LoadedSprites[armorClass];
            }
        }
    }


    public class ArmorComponentPatch : ModulePatch
    {
        private static EBodyPartColliderType[] heads = { EBodyPartColliderType.Eyes, EBodyPartColliderType.Ears, EBodyPartColliderType.Jaw, EBodyPartColliderType.BackHead, EBodyPartColliderType.NeckFront, EBodyPartColliderType.HeadCommon, EBodyPartColliderType.ParietalHead };

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.InventoryLogic.ArmorComponent).GetConstructor(new Type[] { typeof(Item), typeof(ArmorCompTemplate), typeof(RepairableComponent), typeof(BuffComponent) });
        }


        [PatchPostfix]
        private static void PatchPostfix(ArmorComponent __instance)
        {
            if (__instance.ArmorColliders.Intersect(heads).Any())
            {
                List<ItemAttributeClass> canADSAtt = __instance.Item.Attributes;
                ItemAttributeClass canADSAttAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.CanAds);
                canADSAttAttClass.Name = ENewItemAttributeId.CanAds.GetName();
                canADSAttAttClass.StringValue = () => GearStats.AllowsADS(__instance.Item).ToString();
                canADSAttAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                canADSAtt.Add(canADSAttAttClass);
            }

            if (Plugin.ServerConfig.realistic_ballistics)
            {
                bool canSpall = GearStats.CanSpall(__instance.Item);

                List<ItemAttributeClass> bluntAtt = __instance.Item.Attributes;
                ItemAttributeClass bluntAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.BluntThroughput);
                bluntAttClass.Name = ENewItemAttributeId.BluntThroughput.GetName();
                bluntAttClass.StringValue = () => ((1 - __instance.BluntThroughput) * 100).ToString() + " %";
                bluntAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                bluntAtt.Add(bluntAttClass);

                List<ItemAttributeClass> canSpallAtt = __instance.Item.Attributes;
                ItemAttributeClass canSpallAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.CanSpall);
                canSpallAttClass.Name = ENewItemAttributeId.CanSpall.GetName();
                canSpallAttClass.StringValue = () => canSpall.ToString();
                canSpallAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                canSpallAtt.Add(canSpallAttClass);

                if (canSpall)
                {
                    List<ItemAttributeClass> spallReductAtt = __instance.Item.Attributes;
                    ItemAttributeClass spallReductAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.SpallReduction);
                    spallReductAttClass.Name = ENewItemAttributeId.SpallReduction.GetName();
                    spallReductAttClass.StringValue = () => ((1 - GearStats.SpallReduction(__instance.Item)) * 100).ToString() + " %";
                    spallReductAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    spallReductAtt.Add(spallReductAttClass);
                }
            }
        }
    }
}
