using Aki.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.Health;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Systems.Effects;
using UnityEngine;
using static ActiveHealthControllerClass;

namespace RealismMod
{

    public static class MedProperties 
    {

        public static string MedType(Item med)
        {
            if (Utils.NullCheck(med.ConflictingItems))
            {
                return "unknown";
            }
            return med.ConflictingItems[2];
        }

        public static string HBleedHealType(Item med)
        {
            if (Utils.NullCheck(med.ConflictingItems))
            {
                return "unknown";
            }
            return med.ConflictingItems[2];
        }
    }

    //for out of raid healing
    public class HCApplyItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthControllerClass).GetMethod("ApplyItem", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPostfix]
        private static void PatchPostFix(HealthControllerClass __instance)
        {


            Logger.LogWarning("Health Controller");
    
        }
    }

    //in-raid healing
    public class GClass2106ApplyItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2106).GetMethod("ApplyItem", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(GClass2106 __instance, Item item, EBodyPart bodyPart, ref bool __result)
        {
            MedsClass medsClass;
            FoodClass foodClass;
            if (__instance.Player.IsYourPlayer && __instance.CanApplyItem(item, bodyPart))
            {
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance.Player);

                Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
                Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
                Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
                Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
                Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
                Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
                Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

                if (((medsClass = (item as MedsClass)) != null)) 
                {

                    //will have to make mask exception for moustache and similar
                    if (((bodyPart == EBodyPart.Chest || bodyPart == EBodyPart.Stomach) && (vest != null || tacrig != null || bag != null)) || (bodyPart == EBodyPart.Head && (head != null || glasses != null || face != null || ears != null)))
                    {
                        __result = false;
                        return false;
                    }


                    //body part
                    //current effects on that body part
                    //medical item
                    Logger.LogWarning("==============");
                    Logger.LogWarning("GClass2106");

                    IEnumerable<IEffect> effects = __instance.Player.ActiveHealthController.GetAllEffects(bodyPart);

                    foreach (var effect in effects)
                    {
                        if (effect.Type == typeof(GInterface190) && (bodyPart == EBodyPart.Chest || bodyPart == EBodyPart.Stomach || bodyPart == EBodyPart.Head) && MedProperties.HBleedHealType(item) == "trnqt") 
                        {
                            __result = false;
                            return false;
                        }

                        Logger.LogWarning("==");
                        Logger.LogWarning("effect type " + effect.Type);
                        Logger.LogWarning("effect body part " + effect.BodyPart);
                        Logger.LogWarning("==");
                    }

                    Logger.LogWarning("item = " + item.TemplateId);
                    Logger.LogWarning("item name = " + item.LocalizedName());
                    Logger.LogWarning("EBodyPart = " + bodyPart);
                    Logger.LogWarning("==============");
                    return true;

                }
                if((foodClass = (item as FoodClass)) != null)
                {
           

                    FaceShieldComponent fsComponent = __instance.Player.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = __instance.Player.NightVisionObserver.Component;
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

                    //will have to make mask exception for moustache, balaclava etc.
                    if (fsIsON || nvgIsOn || face != null) 
                    {
                        Logger.LogWarning("juice denied");
                        __result = false;
                        return false;
                    }
                    Logger.LogWarning("juice time");
                }

                return true;
            }
            return true;


        }
    }

    //controls whether or not to highlight a part if it can be healed when hovering over it with med item
    public class HealthBarButtonApplyItemPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthBarButton).GetMethod("OnPointerEnter", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPostfix]
        private static void PatchPostFix(HealthBarButton __instance)
        {

            Logger.LogWarning("HealthBarButton OnPointerEnter");
        }
    }

    //when using quickslot
    public class SetQuickSlotItem : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("SetQuickSlotItem", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPostfix]
        private static void PatchPostFix(EFT.Player __instance)
        {


   

            Logger.LogWarning("SetQuickSlotItem");
        }
    }


    /*    var effects = __instance.Player.ActiveHealthController.BodyPartEffects.Effects;

        foreach (var kvp in effects)
        {
            EBodyPart bp = kvp.Key;
            Dictionary<string, float> innerDict = kvp.Value;

            foreach (var innerKvp in innerDict)
            {
                string innerKey = innerKvp.Key;

                Logger.LogWarning("Body part: " + bp + ", Effect: " + innerKey);
            }
        }*/


    /*    public class GClass2108ApplyItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass2108).GetMethod("ApplyItem", BindingFlags.Instance | BindingFlags.Public);

            }
            [PatchPostfix]
            private static void PatchPostFix()
            {

                Logger.LogWarning("GClass2108");
            }
        }*/



    /*    public class HealthBarButtonApplyItemPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(HealthBarButton).GetMethod("OnDrop", BindingFlags.Instance | BindingFlags.Public);

            }
            [PatchPostfix]
            private static void PatchPostFix()
            {

                Logger.LogWarning("HealthBarButton OnDrop");
            }
        }*/

    /*    public class GClass397Patch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass397).GetMethod("TryApplyToCurrentPart", BindingFlags.Instance | BindingFlags.Public);

            }
            [PatchPostfix]
            private static void PatchPostFix()
            {

                Logger.LogWarning("TryApplyToCurrentPart GClass397");
            }
        }

        public class GClass397SetRandomPartToHealPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass397).GetMethod("SetRandomPartToHeal", BindingFlags.Instance | BindingFlags.Public);

            }
            [PatchPostfix]
            private static void PatchPostFix()
            {

                Logger.LogWarning("SetRandomPartToHeal GClass397");
            }
        }

        public class GClass400SetRandomPartToHealPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass400).GetMethod("SetRandomPartToHeal", BindingFlags.Instance | BindingFlags.Public);

            }
            [PatchPostfix]
            private static void PatchPostFix()
            {

                Logger.LogWarning("SetRandomPartToHeal GClass400");
            }
        }

        public class GClass400Patch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass400).GetMethod("ApplyToCurrentPart", BindingFlags.Instance | BindingFlags.Public);

            }
            [PatchPostfix]
            private static void PatchPostFix()
            {

                Logger.LogWarning("ApplyToCurrentPart GClass400");
            }
        }*/

}
