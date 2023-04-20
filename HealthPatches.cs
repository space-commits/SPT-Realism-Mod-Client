using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.Health;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Systems.Effects;
using UnityEngine;
using UnityEngine.Assertions;
using static ActiveHealthControllerClass;
using static EFT.Player;
using static Systems.Effects.Effects;

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
            return med.ConflictingItems[1];
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

    public static class RealismHealthController 
    {
        public static void PlayerInjuryStateCheck(Player player, ManualLogSource logger)
        {
            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);
            bool tremor = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.Tremor);

            PlayerProperties.RightArmBlacked = rightArmDamaged;
            PlayerProperties.LeftArmBlacked = leftArmDamaged;

            if (!rightArmDamaged && !leftArmDamaged && !tremor)
            {
                PlayerProperties.AimMoveSpeedBase = 0.42f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1f;
                PlayerProperties.ADSInjuryMulti = 1f;
                PlayerProperties.StanceInjuryMulti = 1f;
                PlayerProperties.ReloadInjuryMulti = 1f;
                PlayerProperties.RecoilInjuryMulti = 1f;
            }
            if (tremor == true)
            {
                PlayerProperties.AimMoveSpeedBase = 0.4f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1.15f;
                PlayerProperties.ADSInjuryMulti = 0.85f;
                PlayerProperties.StanceInjuryMulti = 0.85f;
                PlayerProperties.ReloadInjuryMulti = 0.9f;
                PlayerProperties.RecoilInjuryMulti = 1.025f;
            }
            if ((rightArmDamaged == true && !leftArmDamaged))
            {
                PlayerProperties.AimMoveSpeedBase = 0.38f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1.5f;
                PlayerProperties.ADSInjuryMulti = 0.51f;
                PlayerProperties.StanceInjuryMulti = 0.6f;
                PlayerProperties.ReloadInjuryMulti = 0.85f;
                PlayerProperties.RecoilInjuryMulti = 1.05f;
            }
            if ((!rightArmDamaged && leftArmDamaged == true))
            {
                PlayerProperties.AimMoveSpeedBase = 0.34f;
                PlayerProperties.ErgoDeltaInjuryMulti = 2f;
                PlayerProperties.ADSInjuryMulti = 0.59f;
                PlayerProperties.StanceInjuryMulti = 0.7f;
                PlayerProperties.ReloadInjuryMulti = 0.8f;
                PlayerProperties.RecoilInjuryMulti = 1.1f;
            }
            if (rightArmDamaged == true && leftArmDamaged == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    logger.LogWarning("both arms damaged");
                }
                PlayerProperties.AimMoveSpeedBase = 0.3f;
                PlayerProperties.ErgoDeltaInjuryMulti = 3.5f;
                PlayerProperties.ADSInjuryMulti = 0.42f;
                PlayerProperties.StanceInjuryMulti = 0.5f;
                PlayerProperties.ReloadInjuryMulti = 0.75f;
                PlayerProperties.RecoilInjuryMulti = 1.15f;
            }
        }

        public static void CanConsume(ManualLogSource Logger, Player player, Item item, ref bool canUse) 
        {
            EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            //will have to make mask exception for moustache, balaclava etc.
            if (fsIsON || nvgIsOn || face != null)
            {
                Logger.LogWarning("juice denied");
                canUse = false;
                return;
            }
            Logger.LogWarning("juice time");
        }

        public static void CanUseMedItem(ManualLogSource Logger, Player player, EBodyPart bodyPart, Item item, ref bool canUse) 
        {

            if (item.Template.Parent._id == "5448f3a64bdc2d60728b456a" || MedProperties.MedType(item) != "drug")  
            {
                Logger.LogWarning("Is drug/stim");
                return;
            }

            EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);

            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
            Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
            Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
            Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
            Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

            bool hasBodyGear = head != null && ears != null && face != null;
            bool hasHeadGear = vest != null && tacrig != null && bag != null;

            bool isHead = bodyPart == EBodyPart.Chest || bodyPart == EBodyPart.Stomach;
            bool isBody = bodyPart == EBodyPart.Chest || bodyPart == EBodyPart.Stomach;
            bool isNotLimb = bodyPart == EBodyPart.Chest || bodyPart == EBodyPart.Stomach || bodyPart == EBodyPart.Head;

            MedsClass med = item as MedsClass;

            //will have to make mask exception for moustache and similar
            if ((isBody && hasBodyGear) || (isHead && hasHeadGear))
            {
                canUse = false;
                return;
            }

            Logger.LogWarning("==============");
            Logger.LogWarning("GClass2106");

            IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(bodyPart);
            bool hasHeavyBleed = effects.OfType<GInterface191>().Any();
            bool hasLightBleed = effects.OfType<GInterface190>().Any();
            bool hasFracture = effects.OfType<GInterface193>().Any();

            if (hasHeavyBleed && isNotLimb && MedProperties.HBleedHealType(item) == "trnqt")
            {
                canUse = false;
                return;
            }

            if (MedProperties.MedType(item) == "splint" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && isNotLimb)
            {
                canUse = false;
                return;
            }

            if (MedProperties.MedType(item) == "medkit" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && hasFracture && !hasHeavyBleed && !hasLightBleed && isNotLimb)
            {
                canUse = false;
                return;
            }

            foreach (IEffect effect in effects)
            {
                Logger.LogWarning("==");
                Logger.LogWarning("effect type " + effect.Type);
                Logger.LogWarning("effect body part " + effect.BodyPart);
                Logger.LogWarning("==");
            }

            Logger.LogWarning("item = " + item.TemplateId);
            Logger.LogWarning("item name = " + item.LocalizedName());
            Logger.LogWarning("EBodyPart = " + bodyPart);
            Logger.LogWarning("==============");
            return;
        }

        public static void DoubleBleedCheck(ManualLogSource logger,Player player)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(EBodyPart.Common);

            if (commonEffects.OfType<GInterface191>().Any() && commonEffects.OfType<GInterface190>().Any()) 
            {
                logger.LogWarning("H + L Bleed Present Commonly");

                IEnumerable<IEffect> chestEffects = player.ActiveHealthController.GetAllActiveEffects(EBodyPart.Chest);
                bool hasHeavyBleedChest = chestEffects.OfType<GInterface191>().Any();
                bool hasLightBleedChest = chestEffects.OfType<GInterface190>().Any();

                IEnumerable<IEffect> stomachEffects = player.ActiveHealthController.GetAllActiveEffects(EBodyPart.Stomach);
                bool hasHeavyBleedStomach = chestEffects.OfType<GInterface191>().Any();
                bool hasLightBleedStomach = chestEffects.OfType<GInterface190>().Any();

                IEnumerable<IEffect> headEffects = player.ActiveHealthController.GetAllActiveEffects(EBodyPart.Stomach);
                bool hasHeavyBleedHead = headEffects.OfType<GInterface191>().Any();
                bool hasLightBleedHead = headEffects.OfType<GInterface190>().Any();

                IReadOnlyList<GClass2103> effectsList = (IReadOnlyList<GClass2103>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

                for (int i = effectsList.Count - 1; i >= 0; i--) 
                {
                    Type effectType = effectsList[i].Type;
                    EBodyPart effectPart = effectsList[i].BodyPart;
                    ActiveHealthControllerClass.GClass2103 effect = effectsList[i];

                    if (hasHeavyBleedChest && hasLightBleedChest && effectPart == EBodyPart.Chest && effectType == typeof(GInterface190))
                    {
                        logger.LogWarning("removed bleed from " + effectPart);
                        effect.ForceResidue();
                    }
                    if (hasHeavyBleedStomach && hasLightBleedStomach && effectPart == EBodyPart.Stomach && effectType == typeof(GInterface190))
                    {
                        logger.LogWarning("removed bleed from " + effectPart);
                        effect.ForceResidue();
                    }
                    if (hasHeavyBleedHead && hasLightBleedHead && effectPart == EBodyPart.Head && effectType == typeof(GInterface190))
                    {
                        logger.LogWarning("removed bleed from " + effectPart);
                        effect.ForceResidue();
                    }
                }
            }
        }
    }

    //CHECK IF MED ITEM IS DRUG OR STIM, IF SO THEN LET ORIGINAL RUN AND SKIP CHECKS!!!!!!!

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
            bool canUse = true;
            if (__instance.Player.IsYourPlayer && __instance.CanApplyItem(item, bodyPart))
            {
                if (((medsClass = (item as MedsClass)) != null)) 
                {
                    RealismHealthController.CanUseMedItem(Logger, __instance.Player, bodyPart, item, ref canUse);
                }
                if((foodClass = (item as FoodClass)) != null)
                {
                    RealismHealthController.CanConsume(Logger, __instance.Player, item, ref canUse);
                }

                __result = canUse;
                return canUse;
            }
            return true;

        }
    }
    //CHECK IF MED ITEM IS DRUG OR STIM, IF SO THEN LET ORIGINAL RUN AND SKIP CHECKS!!!!!!!

    public class ProceedPatch : ModulePatch
    {

        private static EBodyPart[] bodyParts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightArm, EBodyPart.LeftArm, EBodyPart.RightLeg, EBodyPart.LeftLeg};

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("Proceed", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(MedsClass), typeof(EBodyPart), typeof(Callback<GInterface114>), typeof(int), typeof(bool) }, null);

        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, MedsClass meds, ref EBodyPart bodyPart)
        {
            Logger.LogWarning("checking quickslot med");

            string medType = MedProperties.MedType(meds);

            if (__instance.IsYourPlayer && bodyPart == EBodyPart.Common && medType != "drug" && meds.Template._parent != "5448f3a64bdc2d60728b456a") 
            {
                Logger.LogWarning("med item to check = " + meds.LocalizedName());

                bool proceedWithHealing = false;

                string hBleedHealType = MedProperties.HBleedHealType(meds);

                bool canHealFract = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture);
                bool canHealLBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding);
                bool canHealHBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding);

                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);

                Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
                Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
                Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
                Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
                Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
                Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
                Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

                bool hasBodyGear = vest != null || tacrig != null || bag != null;
                bool hasHeadGear = head != null || ears != null || face != null;


                foreach (EBodyPart part in bodyParts)
                {
                    IEnumerable<IEffect> effects = __instance.ActiveHealthController.GetAllActiveEffects(part);

                    bool hasHeavyBleed = effects.OfType<GInterface191>().Any();
                    bool hasLightBleed = effects.OfType<GInterface190>().Any();
                    bool hasFracture = effects.OfType<GInterface193>().Any();

                    bool isHead = part == EBodyPart.Head;
                    bool isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;
                    bool isNotLimb = part == EBodyPart.Chest || part == EBodyPart.Stomach || part == EBodyPart.Head;
                    bool isLimb = part == EBodyPart.LeftArm || part == EBodyPart.RightArm || part == EBodyPart.LeftLeg || part == EBodyPart.RightLeg;

                    foreach (IEffect effect in effects)
                    {
                        Logger.LogWarning("==");
                        Logger.LogWarning("effect type " + effect.Type);
                        Logger.LogWarning("effect body part " + effect.BodyPart);
                        Logger.LogWarning("==");

                        if ((isBody && hasBodyGear) || (isHead && hasHeadGear))
                        {
                            Logger.LogWarning("Part " + part + " has gear on, skipping");

                            continue;
                        }

                        if (canHealHBleed && hasHeavyBleed)
                        {
                            if ((isBody || isHead) && hBleedHealType == "trnqt")
                            {
                                Logger.LogWarning("Part " + part + " has heavy bleed but med is a trnqt, skipping");

                                continue;
                            }
                            if ((isBody || isHead) && hBleedHealType == "clot")
                            {
                                Logger.LogWarning("Part " + part + " has heavy bleed and med is clotter, choosing " + part);

                                bodyPart = part;
                                break;
                            }

                            Logger.LogWarning("Part " + part + " is the only suitable part with a heavy bleed, choosing " + part);

                            bodyPart = part;
                            break;
                        }
                        if (canHealLBleed && hasLightBleed)
                        {
                            if ((isBody || isHead) && hasHeavyBleed) 
                            {
                                Logger.LogWarning("Part " + part + " has heavy bleed and a light bleed, skipping");
                                continue;
                            }

                            Logger.LogWarning("Part " + part + " is the only suitable part with a light bleed, choosing " + part);

                            bodyPart = part;
                            break;
                        }
                        if (hasFracture && canHealFract)
                        {
                            if ((isBody || isHead))
                            {
                                Logger.LogWarning("Part " + part + " has fracture but can't which can't be healed, skipping");
                                continue;
                            }

                            Logger.LogWarning("Part " + part + " is the only suitable part with a fracture, choosing " + part);
                            bodyPart = part;
                            break;
                        }
                    }
                }

                proceedWithHealing = bodyPart != EBodyPart.Common;

                Logger.LogWarning("Prefix Body part = " + bodyPart);
                Logger.LogWarning("proceedWithHealing = " + proceedWithHealing);

                return proceedWithHealing;
            }

            //check all body parts individually for what wounds they have
            //based on what the med item is, assess what it should be healing, and what it can heal. EG if it's a clotter, and heavy bleed on chest and it's covered in gear, skip to nex possible limb
            //if blocked/unable to treat, go to next limb, and change bodypart to that limb
            //if none can be found, skip method entirely.
            

            return true;
        }

        [PatchPostfix]
        private static void PatchPostFix(EBodyPart bodyPart)
        {
            Logger.LogWarning("PostFix Body part = " + bodyPart);
        }
    }

    public class TryProceedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("TryProceed", BindingFlags.Instance | BindingFlags.NonPublic);

        }
        [PatchPostfix]
        private static void PatchPostfix()
        {

        }
    }

    //when using quickslot
    public class SetQuickSlotItem : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("SetQuickSlotItem", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(EFT.Player __instance, EBoundItem quickSlot)
        {
            Logger.LogWarning("SetQuickSlotItem");
            InventoryControllerClass inventoryCont = (InventoryControllerClass)AccessTools.Property(typeof(EFT.Player), "GClass2417_0").GetValue(__instance);
            Item boundItem = inventoryCont.Inventory.FastAccess.GetBoundItem(quickSlot);
            FoodClass food = boundItem as FoodClass;
            if (boundItem != null && (food = (boundItem as FoodClass)) != null)
            {
                bool canUse = true;
                RealismHealthController.CanConsume(Logger, __instance, boundItem, ref canUse);
                Logger.LogWarning("qucik slot, can use = " + canUse);
                return canUse;
            }
            return true;
        }
    }


    //controls whether or not to highlight a part if it can be healed when hovering over it with med item
    public class HealthBarButtonApplyItemPatch : ModulePatch
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
