using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using AmplifyMotion;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI.Health;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Systems.Effects;
using UnityEngine;
using UnityEngine.Assertions;
using static ActiveHealthControllerClass;
using static CW2.Animations.PhysicsSimulator.Val;
using static EFT.Player;
using static Systems.Effects.Effects;

namespace RealismMod
{
    //in-raid healing
    public class ApplyItemPatch : ModulePatch
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
                    if (Plugin.EnableLogging.Value) 
                    {
                        Logger.LogWarning("ApplyItem Med");
                    }

                    RealismHealthController.CanUseMedItem(Logger, __instance.Player, bodyPart, item, ref canUse);
                }
                if((foodClass = (item as FoodClass)) != null)
                {
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("ApplyItem Food");
                    }
           
                    if (Plugin.GearBlocksEat.Value) 
                    {
                        RealismHealthController.CanConsume(Logger, __instance.Player, item, ref canUse);
                    }
                }

                __result = canUse;
                return canUse;
            }
            return true;

        }
    }

    public class ProceedPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("Proceed", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(MedsClass), typeof(EBodyPart), typeof(Callback<GInterface114>), typeof(int), typeof(bool) }, null);

        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, MedsClass meds, ref EBodyPart bodyPart)
        {
            string medType = MedProperties.MedType(meds);
            if (__instance.IsYourPlayer && medType != "drug" && meds.Template._parent != "5448f3a64bdc2d60728b456a")
            {
                MedsClass med = meds as MedsClass;
                float medHPRes = med.MedKitComponent.HpResource;

                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("checking if med can proceed");
                    Logger.LogWarning("bodyPart = " + bodyPart);
                    Logger.LogWarning("med item to check = " + meds.LocalizedName());
                    Logger.LogWarning("remaining hp resource = " + medHPRes);
                }

                string hBleedHealType = MedProperties.HBleedHealType(meds);

                bool canHealFract = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && ((medType == "medkit" && medHPRes >= 3) || medType != "medkit");
                bool canHealLBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding);
                bool canHealHBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding) && ((medType == "medkit" && medHPRes >= 3) || medType != "medkit");

                if (bodyPart == EBodyPart.Common)
                {
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("Body part is common");
                    }
              

                    EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);

                    Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
                    Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
                    Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
                    Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
                    Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
                    Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
                    Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

                    bool mouthBlocked = RealismHealthController.MouthIsBlocked(head, face, equipment);

                    bool hasBodyGear = vest != null || tacrig != null || bag != null;
                    bool hasHeadGear = head != null || ears != null || face != null;

                    FaceShieldComponent fsComponent = __instance.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = __instance.NightVisionObserver.Component;
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On) && GearProperties.BlocksMouth(fsComponent.Item);
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

                    if (Plugin.GearBlocksHeal.Value) 
                    {
                        if (medType == "pills" && (mouthBlocked || fsIsON || nvgIsOn))
                        {
                            return false;
                        }
                        else if (medType == "pills")
                        {
                            return true;
                        }
                    }
                 

                    foreach (EBodyPart part in RealismHealthController.BodyParts)
                    {
                        bool hasHeavyBleed = false;
                        bool hasLightBleed = false;
                        bool hasFracture = false;

                        IEnumerable<IEffect> effects = RealismHealthController.GetAllEffectsOnLimb(__instance, part, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

                        bool isHead = false;
                        bool isBody = false;
                        bool isNotLimb = false;

                        RealismHealthController.GetBodyPartType(part, ref isNotLimb, ref isHead, ref isBody);

                        foreach (IEffect effect in effects)
                        {
                            if (Plugin.EnableLogging.Value)
                            {
                                Logger.LogWarning("==");
                                Logger.LogWarning("effect type " + effect.Type);
                                Logger.LogWarning("effect body part " + effect.BodyPart);
                                Logger.LogWarning("==");
                            }
           

                            if (Plugin.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear)))
                            {
                                if (Plugin.EnableLogging.Value)
                                {
                                    Logger.LogWarning("Part " + part + " has gear on, skipping");
                                }

                                continue;
                            }

                            if (canHealHBleed && effect.Type == typeof(GInterface191))
                            {
                                if (!isNotLimb)
                                {
                                    if (Plugin.EnableLogging.Value)
                                    {
                                        Logger.LogWarning("Limb " + part + " has heavy bleed, choosing " + part);
                                    }
                          
                                    bodyPart = part;
                                    break;
                                }
                                if ((isBody || isHead) && hBleedHealType == "trnqt")
                                {
                                    if (Plugin.EnableLogging.Value)
                                    {
                                        Logger.LogWarning("Part " + part + " has heavy bleed but med is a trnqt, skipping");
                                    }

                                    continue;
                                }
                                if ((isBody || isHead) && (hBleedHealType == "clot" || hBleedHealType == "combo" || hBleedHealType == "surg"))
                                {
                                    if (Plugin.EnableLogging.Value)
                                    {
                                        Logger.LogWarning("Part " + part + " has heavy bleed and this bleed heal type can stop it, choosing " + part);
                                    }

                                    bodyPart = part;
                                    break;
                                }
                                if (Plugin.EnableLogging.Value)
                                {
                                    Logger.LogWarning("Part " + part + " has heavy bleed and no other checks fired, choosing " + part);
                                }

                                bodyPart = part;
                                break;
                            }
                            if (canHealLBleed && effect.Type == typeof(GInterface190))
                            {
                                if (!isNotLimb)
                                {
                                    if (Plugin.EnableLogging.Value)
                                    {
                                        Logger.LogWarning("Limb " + part + " has light bleed, choosing " + part);
                                    }

                                    bodyPart = part;
                                    break;
                                }
                                if ((isBody || isHead) && hasHeavyBleed)
                                {
                                    if (Plugin.EnableLogging.Value)
                                    {
                                        Logger.LogWarning("Part " + part + " has heavy bleed and a light bleed, skipping");
                                    }

                                    continue;
                                }
                                if (Plugin.EnableLogging.Value)
                                {
                                    Logger.LogWarning("Part " + part + " has light bleed and no other checks fired, choosing " + part);
                                }

                                bodyPart = part;
                                break;
                            }
                            if (canHealFract && effect.Type == typeof(GInterface193))
                            {
                                if (!isNotLimb)
                                {
                                    if (Plugin.EnableLogging.Value)
                                    {
                                        Logger.LogWarning("Limb " + part + " has a fracture, choosing " + part);
                                    }

                                    bodyPart = part;
                                    break;
                                }
                                if (isNotLimb)
                                {
                                    if (Plugin.EnableLogging.Value)
                                    {
                                        Logger.LogWarning("Part " + part + " has fracture which can't be healed, skipping");
                                    }

                                    continue;
                                }
                                if (Plugin.EnableLogging.Value)
                                {
                                    Logger.LogWarning("Part " + part + " has fracture and no other checks fired, choosing " + part);
                                }

                                bodyPart = part;
                                break;
                            }
                        }

                        if (bodyPart != EBodyPart.Common)
                        {
                            if (Plugin.EnableLogging.Value)
                            {
                                Logger.LogWarning("Common Body Part replaced with " + bodyPart);
                            }
                            break;
                        }
                    }

                    if (bodyPart == EBodyPart.Common) 
                    {
                        if (Plugin.EnableLogging.Value)
                        {
                            Logger.LogWarning("After all checks, body part is still common, canceling heal");
                        }
                        return false;
                    }
                }

                //determine if any effects should be applied based on what is being healed
                if (bodyPart != EBodyPart.Common)
                {
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("Checking if custom effects should be applied");
                    }

                    bool hasHeavyBleed = false;
                    bool hasLightBleed = false;
                    bool hasFracture = false;

                    IEnumerable<IEffect> effects = RealismHealthController.GetAllEffectsOnLimb(__instance, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);
                     
                    bool isHead = false;
                    bool isBody = false;
                    bool isNotLimb = false;

                    RealismHealthController.GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

                    if (Plugin.TrnqtEffect.Value && hasHeavyBleed && canHealHBleed && (hBleedHealType == "combo" || hBleedHealType == "trnqt") && !isNotLimb)
                    {
                        if (Plugin.EnableLogging.Value)
                        {
                            Logger.LogWarning("Tourniquet application detected, adding TourniquetEffect");
                        }

                        TourniquetEffect trnqt = new TourniquetEffect(MedProperties.HpPerTick(meds), null, bodyPart, __instance, meds.HealthEffectsComponent.UseTime);
                        RealismHealthController.AddCustomEffect(trnqt, false);
                    }

                    //need to add tunnel vision, pain
                    if (medType == "surg") 
                    {
                        SurgeryEffect surg = new SurgeryEffect(MedProperties.HpPerTick(meds), null, bodyPart, __instance, meds.HealthEffectsComponent.UseTime);
                        RealismHealthController.AddCustomEffect(surg, false);
                        if (Plugin.EnableLogging.Value)
                        {
                            Logger.LogWarning("Surgery kit use detected, adding SurgeryEffect");
                        }
                    }
                }
            }


            //IF PART IS NOT COMMON, NOT DRUGS/STIMS, AND MEDKIT COULD HAD HEALED HEAVY BLEED, AND SELECTED LIMB AS A HEAVY BLEED

            return true;
        }
    }


    //when using quickslot
    public class SetQuickSlotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("SetQuickSlotItem", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(EFT.Player __instance, EBoundItem quickSlot)
        {
            InventoryControllerClass inventoryCont = (InventoryControllerClass)AccessTools.Property(typeof(EFT.Player), "GClass2417_0").GetValue(__instance);
            Item boundItem = inventoryCont.Inventory.FastAccess.GetBoundItem(quickSlot);
            FoodClass food = boundItem as FoodClass;
            if (boundItem != null && (food = (boundItem as FoodClass)) != null)
            {
                bool canUse = true;
                RealismHealthController.CanConsume(Logger, __instance, boundItem, ref canUse);
                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("qucik slot, can use = " + canUse);
                }
          
                return canUse;
            }
            return true;
        }
    }

    public class RemoveEffectPatch : ModulePatch
    {

        private static Type _targetType;
        private static MethodInfo _targetMethod;

        public RemoveEffectPatch()
        {
            _targetType = AccessTools.TypeByName("MedsController");
           /* _targetMethod = _targetType.GetMethod("Remove");*/
            _targetMethod = AccessTools.Method(_targetType, "Remove");

        }

        protected override MethodBase GetTargetMethod()
        {
            return _targetMethod;
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("Cancelling Meds");
            }

            RealismHealthController.CancelEffects(Logger);
        }
    }


    public class EnergyRatePatch : ModulePatch
    {

        private static Type _targetType;
        private static MethodInfo _targetMethod;

        public EnergyRatePatch()
        {
            _targetType = AccessTools.TypeByName("Existence");
            _targetMethod = AccessTools.Method(_targetType, "method_5");
        }

        protected override MethodBase GetTargetMethod()
        {
            return _targetMethod;
        }

        private static float GetDecayRate(Player player)
        {
            float energyDecayRate = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyLoopTime;
            if (player.HealthController.IsBodyPartDestroyed(EBodyPart.Stomach))
            {
                energyDecayRate /= Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.DestroyedStomachEnergyTimeFactor;
            }
            return energyDecayRate;
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result)
        {
            if (Utils.IsReady && !Utils.IsInHideout()) 
            {
                Player player = Utils.GetPlayer();
                if (player.IsYourPlayer) 
                {
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("EnergyRatePatch");
                    }
     
                    float num = 1f - player.Skills.HealthHydration;
                    __result = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyDamage * num * PlayerProperties.HealthResourceRateFactor / GetDecayRate(player);
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("modified energy decay = " + __result);
                        Logger.LogWarning("original energy decay = " + Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyDamage * num / GetDecayRate(player));
                    }

                    return false;
                }
            }
            return true;
 
        }
    }


    public class HydoRatePatch : ModulePatch
    {
        private static Type _targetType;
        private static MethodInfo _targetMethod;

        public HydoRatePatch()
        {
            _targetType = AccessTools.TypeByName("Existence");
            _targetMethod = AccessTools.Method(_targetType, "method_6");
        }

        protected override MethodBase GetTargetMethod()
        {
            return _targetMethod;
        }

        private static float GetDecayRate(Player player)
        {
            float energyDecayRate = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.HydrationLoopTime;
            if (player.HealthController.IsBodyPartDestroyed(EBodyPart.Stomach))
            {
                energyDecayRate /= Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.DestroyedStomachHydrationTimeFactor;
            }
            return energyDecayRate;
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result)
        {
            if (Utils.IsReady && !Utils.IsInHideout())
            {
                Player player = Utils.GetPlayer();
                if (player.IsYourPlayer)
                {
                    float num = 1f - player.Skills.HealthHydration;
                    __result = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.HydrationDamage * num * PlayerProperties.HealthResourceRateFactor / GetDecayRate(player);
                    return false;
                }
            }
            return true;
        }
    }

    public class StamRegenRatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass704).GetMethod("method_21", BindingFlags.Instance | BindingFlags.NonPublic);

        }
        [PatchPrefix]
        private static bool Prefix(GClass704 __instance, float baseValue, ref float __result)
        {
            float[] float_7 = (float[])AccessTools.Field(typeof(GClass704), "float_7").GetValue(__instance);
            GClass704.EPose epose_0 = (GClass704.EPose)AccessTools.Field(typeof(GClass704), "epose_0").GetValue(__instance);
            Player player_0 = (Player)AccessTools.Field(typeof(GClass704), "player_0").GetValue(__instance);
            float Single_0 = (float)AccessTools.Property(typeof(GClass704), "Single_0").GetValue(__instance);

            __result = baseValue * float_7[(int)epose_0] * Singleton<BackendConfigSettingsClass>.Instance.StaminaRestoration.GetAt(player_0.HealthController.Energy.Normalized) * (player_0.Skills.EnduranceBuffRestoration + 1f) * PlayerProperties.HealthStamRegenFactor / Single_0;
            return false;
        }
    }

    public class ReceiveDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("ReceiveDamage", BindingFlags.Instance | BindingFlags.NonPublic);

        }
        [PatchPostfix]
        private static void PatchPostfix(Player __instance, float damage, EBodyPart part, EDamageType type, float absorbed, MaterialType special)
        {
            if (__instance.IsYourPlayer == true)
            {
                //get damage type, amount and part
                //difference checks for each type of damage
                //for fall dmg, only record if past
                //get time of damage received
                //if min time reached, start regen
                //if certain types of damage received, cancel healing.
            }
        }
    }


    /*    //controls whether or not to highlight a part if it can be healed when hovering over it with med item
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
        }*/


    /*    //for out of raid healing
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
        }*/

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
