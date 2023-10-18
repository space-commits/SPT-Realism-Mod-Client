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
using BPConstructor = GClass2496;
using BPTemplate = GClass2402;
using RigConstructor = GClass2497;
using RigTemplate = GClass2403;
using HeadsetClass = GClass2451;
using HeadsetTemplate = GClass2357;
using ArmorCompTemplate = GInterface233;

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

            float comfortModifier = GearProperties.ComfortModifier(item);
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

            float gearReloadSpeed = GearProperties.ReloadSpeedMulti(item);
            float reloadSpeedPercent = (float)Math.Round((gearReloadSpeed - 1f) * 100f);

            float comfortModifier = GearProperties.ComfortModifier(item);
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

    public class GearPatches
    {

        public class ArmorZoneBaseDisplayPatch : ModulePatch
        {

            private static Type _targetType;
            private static MethodInfo _method_0;

            public ArmorZoneBaseDisplayPatch()
            {
                _targetType = PatchConstants.EftTypes.Single(IsType);
                _method_0 = _targetType.GetMethod("method_0", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            protected override MethodBase GetTargetMethod()
            {
                return _method_0;
            }

            private static bool IsType(Type type)
            {
                return type.GetField("armorComponent_0") != null && type.GetField("item") != null;
            }

            private static string GetItemClass(CompositeArmorComponent x)
            {
                return x.Item.ShortName.Localized(null) + ": " + GearProperties.ArmorClass(x.Item);
            }

            [PatchPrefix]
            private static bool Prefix(ref float __result, ref EFT.InventoryLogic.ArmorComponent ___armorComponent_0)
            {
                float armorElementsToAdd = 0;

                if (GearProperties.HasExtraArmor(___armorComponent_0.Item))
                {
                    armorElementsToAdd += 1;
                }
                if (GearProperties.HasNeckArmor(___armorComponent_0.Item)) 
                {
                    armorElementsToAdd += 1;
                }
                if (GearProperties.HasSideArmor(___armorComponent_0.Item))
                {
                    armorElementsToAdd += 1;
                }
                if (GearProperties.HasStomachArmor(___armorComponent_0.Item))
                {
                    armorElementsToAdd += 1;
                }
  
                __result = ___armorComponent_0.ArmorZone.Contains(EBodyPart.Stomach) ? (float)___armorComponent_0.ArmorZone.Length + armorElementsToAdd - 1f : (float)___armorComponent_0.ArmorZone.Length + armorElementsToAdd;
                return false;
            }
        }

        public class ArmorZoneSringValueDisplayPatch : ModulePatch
        {

            private static Type _targetType;    
            private static MethodInfo _method_1;

            public ArmorZoneSringValueDisplayPatch()
            {
                _targetType = PatchConstants.EftTypes.Single(IsType);
                _method_1 = _targetType.GetMethod("method_1", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            protected override MethodBase GetTargetMethod()
            {
                return _method_1;
            }

            private static bool IsType(Type type)
            {
                return type.GetField("armorComponent_0") != null && type.GetField("item") != null;
            }

            private static string GetItemClass(CompositeArmorComponent x)
            {
                return x.Item.ShortName.Localized(null) + ": " + GearProperties.ArmorClass(x.Item);
            }

            [PatchPrefix]
            private static bool Prefix(ref string __result, ref EFT.InventoryLogic.ArmorComponent ___armorComponent_0)
            {
                if (___armorComponent_0.ArmorZone.Contains(EBodyPart.Head)) 
                {
                    return true;
                }

                List<string> parts = new List<string>();

                foreach (EBodyPart e in ___armorComponent_0.ArmorZone) 
                {
                    if (e != EBodyPart.Stomach) 
                    {
                        parts.Add(e.ToString());
                    }
                }
                if (GearProperties.HasExtraArmor(___armorComponent_0.Item))
                {
                    Logger.LogWarning(___armorComponent_0.Item.LocalizedName());
                    Logger.LogWarning("has secondary armor");
                    parts.Add("SECONDARY ARMOR");
                }
                if (GearProperties.HasNeckArmor(___armorComponent_0.Item))
                {
                    parts.Add("NECK");
                }
                if (GearProperties.HasSideArmor(___armorComponent_0.Item))
                {
                    parts.Add("SIDES");
                }
                if (GearProperties.HasStomachArmor(___armorComponent_0.Item))
                {
                    parts.Add("STOMACH");
                }

                __result =  Enumerable.Cast<object>(parts).CastToStringValue("\n", true);
                return false;
            }
        }


        public class ArmorClassDisplayPatch : ModulePatch
        {

            private static Type _targetType;
            private static MethodInfo _method_4;

            public ArmorClassDisplayPatch()
            {
                _targetType = PatchConstants.EftTypes.Single(IsType);
                _method_4 = _targetType.GetMethod("method_4", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            protected override MethodBase GetTargetMethod()
            {
                return _method_4;
            }

            private static bool IsType(Type type)
            {
                return type.GetField("armorComponent_0") != null && type.GetField("item") != null;
            }

            private static string GetItemClass(CompositeArmorComponent x)
            {
                return x.Item.ShortName.Localized(null) + ": " + GearProperties.ArmorClass(x.Item);
            }

            [PatchPrefix]
            private static bool Prefix(ref string __result, ref EFT.InventoryLogic.ArmorComponent ___armorComponent_0)
            {
                CompositeArmorComponent[] array = ___armorComponent_0.Item.GetItemComponentsInChildren<CompositeArmorComponent>(true).ToArray<CompositeArmorComponent>();

                if (array.Length > 1)
                {
                    __result = array.Select(new Func<CompositeArmorComponent, string>(GetItemClass)).CastToStringValue("\n", true);
                    return false;
                }

                __result = GearProperties.ArmorClass(___armorComponent_0.Item);
                return false;
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

                float dB = GearProperties.DbLevel(item);

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

        public class ArmorComponentPatch : ModulePatch
        {

            protected override MethodBase GetTargetMethod()
            {
                return typeof(EFT.InventoryLogic.ArmorComponent).GetConstructor(new Type[] { typeof(Item), typeof(ArmorCompTemplate), typeof(RepairableComponent), typeof(BuffComponent) });
            }


            [PatchPostfix]
            private static void PatchPostfix(ArmorComponent __instance)
            {

                bool showADS = false;
                EBodyPart[] zones = __instance.ArmorZone;

                foreach (EBodyPart part in zones)
                {
                    if (part == EBodyPart.Head)
                    {
                        showADS = true;
                    }
                }

                if (showADS == true)
                {
                    List<ItemAttributeClass> canADSAtt = __instance.Item.Attributes;
                    ItemAttributeClass canADSAttAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.CanAds);
                    canADSAttAttClass.Name = ENewItemAttributeId.CanAds.GetName();
                    canADSAttAttClass.StringValue = () => GearProperties.AllowsADS(__instance.Item).ToString();
                    canADSAttAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    canADSAtt.Add(canADSAttAttClass);
                }

                if (Plugin.ModConfig.realistic_ballistics == true)
                {
                    bool canSpall = GearProperties.CanSpall(__instance.Item);

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

                    if (canSpall == true)
                    {
                        List<ItemAttributeClass> spallReductAtt = __instance.Item.Attributes;
                        ItemAttributeClass spallReductAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.SpallReduction);
                        spallReductAttClass.Name = ENewItemAttributeId.SpallReduction.GetName();
                        spallReductAttClass.StringValue = () => ((1 - GearProperties.SpallReduction(__instance.Item)) * 100).ToString() + " %";
                        spallReductAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        spallReductAtt.Add(spallReductAttClass);
                    }
                }
            }
        }
    }
}
