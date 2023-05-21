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
using static RealismMod.Attributes;
using static Systems.Effects.Effects;

namespace RealismMod
{

    public class MedkitConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MedKitComponent).GetConstructor(new Type[] { typeof(Item), typeof(GInterface244) });
        }

        private static string getHBTypeString(string type) 
        {
            switch (type) 
            {
                case "trnqt":
                    return "TOURNIQUET";
                case "surg": 
                    return "SURGICAL";
                case "combo":
                    return "TOURNIQUET + CHEST SEAL";
                case "clot":
                    return "CLOTTING AGENT";
                default:
                    return "NONE";
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(MedKitComponent __instance, Item item)
        {
            string medType = MedProperties.MedType(item);

            if (medType == "trnqt" || medType == "medkit" || medType == "surg")
            {
                string hBleedType = MedProperties.HBleedHealType(item);
                float hpPerTick = medType != "surg" ? -MedProperties.HpPerTick(item) : MedProperties.HpPerTick(item);

                if (hBleedType != "none") 
                {
                    List<ItemAttributeClass> hbAtt = item.Attributes;
                    ItemAttributeClass hbAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.HBleedType);
                    hbAttClass.Name = ENewItemAttributeId.HBleedType.GetName();
                    hbAttClass.StringValue = () => getHBTypeString(hBleedType);
                    hbAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    hbAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                    hbAttClass.LessIsGood = false;
                    hbAtt.Add(hbAttClass);

                    if (medType == "surg")
                    {
                        List<ItemAttributeClass> hpTickAtt = item.Attributes;
                        ItemAttributeClass hpAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.HpPerTick);
                        hpAttClass.Name = ENewItemAttributeId.HpPerTick.GetName();
                        hpAttClass.Base = () => hpPerTick;
                        hpAttClass.StringValue = () => hpPerTick.ToString();
                        hpAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        hpAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                        hpAttClass.LessIsGood = false;
                        hpTickAtt.Add(hpAttClass);

                        List<ItemAttributeClass> trqntAtt = item.Attributes;
                        ItemAttributeClass trnqtClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.RemoveTrnqt);
                        trnqtClass.Name = ENewItemAttributeId.RemoveTrnqt.GetName();
                        trnqtClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        trqntAtt.Add(trnqtClass);
                    }
                    else 
                    {
                        List<ItemAttributeClass> hpTickAtt = item.Attributes;
                        ItemAttributeClass hpAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.LimbHpPerTick);
                        hpAttClass.Name = ENewItemAttributeId.LimbHpPerTick.GetName();
                        hpAttClass.Base = () => hpPerTick;
                        hpAttClass.StringValue = () => hpPerTick.ToString();
                        hpAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        hpAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                        hpAttClass.LessIsGood = false;
                        hpTickAtt.Add(hpAttClass);
                    }
                }
            }
        }
    }


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


    public class RestoreBodyPartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ActiveHealthControllerClass).GetMethod("RestoreBodyPart", BindingFlags.Instance | BindingFlags.Public);

        }

        private static BodyPartStateWrapper GetBodyPartStateWrapper(ActiveHealthControllerClass instance, EBodyPart bodyPart)
        {

            PropertyInfo bodyPartStateProperty = typeof(ActiveHealthControllerClass).GetProperty("IReadOnlyDictionary_0", BindingFlags.Instance | BindingFlags.NonPublic);
            var bodyPartStateDict = (IDictionary)bodyPartStateProperty.GetMethod.Invoke(instance, null);
            
            object bodyPartStateInstance;
            if (bodyPartStateDict.Contains(bodyPart))
            {
                bodyPartStateInstance = bodyPartStateDict[bodyPart];
            }
            else
            {
                Logger.LogWarning("=======Realism Mod: FAILED TO GET BODYPARTSTATE INSTANCE=========");
                return null;
            }

            return new BodyPartStateWrapper(bodyPartStateInstance);
        }

        [PatchPrefix]
        private static bool Prefix(ref ActiveHealthControllerClass __instance, EBodyPart bodyPart, float healthPenalty, ref bool __result)
        {

            BodyPartStateWrapper bodyPartStateWrapper = GetBodyPartStateWrapper(__instance, bodyPart);
            SkillsClass skills = (SkillsClass)AccessTools.Field(typeof(ActiveHealthControllerClass), "gclass1680_0").GetValue(__instance);
            Action<EBodyPart, ValueStruct> actionStruct = (Action<EBodyPart, ValueStruct>)AccessTools.Field(typeof(ActiveHealthControllerClass), "action_15").GetValue(__instance);
            MethodInfo method_45 = AccessTools.Method(typeof(ActiveHealthControllerClass), "method_45");
            MethodInfo method_38 = AccessTools.Method(typeof(ActiveHealthControllerClass), "method_38");

            if (!bodyPartStateWrapper.IsDestroyed) 
            {
                Logger.LogWarning("body part not destroyed");
                __result = true;
                return false;
            }

            ValueStruct hp = __instance.GetBodyPartHealth(bodyPart);
            float currentHp = hp.Current;
            HealthValue health = bodyPartStateWrapper.Health;
            bodyPartStateWrapper.IsDestroyed = false;
            healthPenalty += (1f - healthPenalty) * skills.SurgeryReducePenalty;
            bodyPartStateWrapper.Health = new HealthValue(1f, Mathf.Max(1f, Mathf.Ceil(bodyPartStateWrapper.Health.Maximum * healthPenalty)), 0f);
            method_45.Invoke(__instance, new object[] { bodyPart, EDamageType.Medicine });
            method_38.Invoke(__instance, new object[] { bodyPart});

            Action<EBodyPart, ValueStruct> action = actionStruct;
            if (action != null)
            {
                action(bodyPart, health.CurrentAndMaximum);
            }
            __result = true;
            return false;
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

    public class RemoveEffectPatch : ModulePatch
    {
        private static Type _targetType;
        private static MethodInfo _targetMethod;

        public RemoveEffectPatch()
        {
            _targetType = AccessTools.TypeByName("MedsController");
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

    public class HCApplyDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ActiveHealthControllerClass).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public);
        }

        private static EDamageType[] acceptedDamageTypes = { EDamageType.HeavyBleeding, EDamageType.LightBleeding, EDamageType.Fall, EDamageType.Barbed, EDamageType.Blunt };

        [PatchPostfix]
        private static void PatchPostfix(ActiveHealthControllerClass __instance, EBodyPart bodyPart, float damage, DamageInfo damageInfo)
        {
            if (__instance.Player.IsYourPlayer)
            {
                Logger.LogWarning("=========");
                Logger.LogWarning("part = " + bodyPart);
                Logger.LogWarning("type = " + damageInfo.DamageType);
                Logger.LogWarning("damage = " + damage);
                Logger.LogWarning("=========");

                EDamageType damageType = damageInfo.DamageType;

                if (acceptedDamageTypes.Contains(damageType))
                {
                    if ((damageType == EDamageType.Fall && damage <= 15f))
                    {
                        DamageTracker.TotalFallDamage = 0f;
                        DamageTracker.AddDamage(damageType, bodyPart, damage);
                    }
                    if (damageType == EDamageType.Blunt && damage <= 10f)
                    {
                        DamageTracker.TotalBluntDamage = 0f;
                        DamageTracker.AddDamage(damageType, bodyPart, damage);
                    }
                    if (damageType == EDamageType.HeavyBleeding || damageType == EDamageType.LightBleeding || damageType == EDamageType.Barbed)
                    {
                        DamageTracker.AddDamage(damageType, bodyPart, damage);
                        Logger.LogWarning("total bleed damage = " + DamageTracker.TotalHeavyBleedDamage);
                    }
                }
            }
        }
    }


    public class ProceedPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("Proceed", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(MedsClass), typeof(EBodyPart), typeof(Callback<GInterface114>), typeof(int), typeof(bool) }, null);

        }

        private static void handleHealthEffects(string medType, MedsClass meds, EBodyPart bodyPart, Player player, string hBleedHealType, bool canHealHBleed, bool canHealLBleed, bool canHealFract)
        {
            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("Checking if custom effects should be applied");
            }

            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
            bool hasFracture = false;

            IEnumerable<IEffect> effects = RealismHealthController.GetAllEffectsOnLimb(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

            bool isHead = false;
            bool isBody = false;
            bool isNotLimb = false;

            RealismHealthController.GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

            MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethod();

            if (Plugin.TrnqtEffect.Value && hasHeavyBleed && canHealHBleed)
            {
                NotificationManagerClass.DisplayMessageNotification("Heavy Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);
                float hpToRestore = Mathf.Min(DamageTracker.TotalHeavyBleedDamage, 25f);

                if ((hBleedHealType == "combo" || hBleedHealType == "trnqt") && !isNotLimb)
                {
                    NotificationManagerClass.DisplayWarningNotification("Tourniquet Applied On " + bodyPart + ", You Are Losing Health On This Limb. Use A Surgery Kit To Remove It.", EFT.Communications.ENotificationDurationType.Long);

                    TourniquetEffect trnqt = new TourniquetEffect(MedProperties.HpPerTick(meds), null, bodyPart, player, meds.HealthEffectsComponent.UseTime);
                    RealismHealthController.AddCustomEffect(trnqt, false);
                    RealismHealthController.TrnqtRestoreHPArossBody(player, hpToRestore, meds.HealthEffectsComponent.UseTime, bodyPart);
                    if (DamageTracker.TotalHeavyBleedDamage > 0f)
                    {
                        RealismHealthController.TrnqtRestoreHPArossBody(player, hpToRestore, meds.HealthEffectsComponent.UseTime, bodyPart);
                    }
                }
                else if (DamageTracker.TotalHeavyBleedDamage > 0f)
                {
                    RealismHealthController.RestoreHPArossBody(player, hpToRestore, meds.HealthEffectsComponent.UseTime);
                }
                DamageTracker.TotalHeavyBleedDamage = Mathf.Max(DamageTracker.TotalHeavyBleedDamage - hpToRestore, 0f);
            }

            if (medType == "surg")
            {
                NotificationManagerClass.DisplayMessageNotification("Surgery Kit Applied On " + bodyPart + ", Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

                if (RealismHealthController.HasEffectOfType(typeof(TourniquetEffect), bodyPart)) 
                {
                    NotificationManagerClass.DisplayMessageNotification("Surgical Kit Used, Removing Any Tourniquet Effect Present On Limb: " + bodyPart, EFT.Communications.ENotificationDurationType.Long);
                }

                SurgeryEffect surg = new SurgeryEffect(MedProperties.HpPerTick(meds), null, bodyPart, player, meds.HealthEffectsComponent.UseTime);
                RealismHealthController.AddCustomEffect(surg, false);
            }

            if (canHealLBleed && hasLightBleed && !hasHeavyBleed && (medType == "trnqt" && !isNotLimb || medType != "trnqt"))
            {
                NotificationManagerClass.DisplayMessageNotification("Light Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);
                float hpToRestore = Mathf.Min(DamageTracker.TotalLightBleedDamage, 15f);

                if (medType == "trnqt" && !isNotLimb) 
                {
                    NotificationManagerClass.DisplayWarningNotification("Tourniquet Applied On " + bodyPart + ", You Are Losing Health On This Limb. Use A Surgery Kit To Remove It.", EFT.Communications.ENotificationDurationType.Long);

                    TourniquetEffect trnqt = new TourniquetEffect(MedProperties.HpPerTick(meds), null, bodyPart, player, meds.HealthEffectsComponent.UseTime);
                    RealismHealthController.AddCustomEffect(trnqt, false);
                    if (DamageTracker.TotalLightBleedDamage > 0f) 
                    {
                        RealismHealthController.TrnqtRestoreHPArossBody(player, hpToRestore, meds.HealthEffectsComponent.UseTime, bodyPart);
                    }
                }
                else if (DamageTracker.TotalLightBleedDamage > 0f) 
                {
                    RealismHealthController.RestoreHPArossBody(player, hpToRestore, meds.HealthEffectsComponent.UseTime);
                }
                DamageTracker.TotalLightBleedDamage = Mathf.Max(DamageTracker.TotalLightBleedDamage - hpToRestore, 0f);
            }

            if (canHealFract && hasFracture && (medType == "splint" || (medType == "medkit" && !hasHeavyBleed && !hasLightBleed)))
            {
                NotificationManagerClass.DisplayMessageNotification("Fracture On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

                HealthRegenEffect regenEffect = new HealthRegenEffect(1f, null, bodyPart, player, meds.HealthEffectsComponent.UseTime, 10f);
                RealismHealthController.AddCustomEffect(regenEffect, false);
            }
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
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

                    if (Plugin.GearBlocksHeal.Value && medType == "pills" && (mouthBlocked || fsIsON || nvgIsOn))
                    {
                        NotificationManagerClass.DisplayWarningNotification("Can't Take Pills, Mouth Is Blocked By Faceshield/NVGs/Mask. Toggle Off Faceshield/NVG Or Remove Mask/Headgear", EFT.Communications.ENotificationDurationType.Long);
                        return false;
                    }
                    else if (medType == "pills")
                    {
                        return true;
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
                                    NotificationManagerClass.DisplayWarningNotification("Tourniquets Can Only Stop Heavy Bleeds On Limbs", EFT.Communications.ENotificationDurationType.Long);

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
                                if ((isBody || isHead) && hBleedHealType == "trnqt")
                                {
                                    if (Plugin.EnableLogging.Value)
                                    {
                                        Logger.LogWarning("Part " + part + " hhas heavy bleed but med is a trnqt, skipping");
                                    }

                                    NotificationManagerClass.DisplayWarningNotification("Tourniquets Can Only Stop Light Bleeds On Limbs", EFT.Communications.ENotificationDurationType.Long);

                                    continue;
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

                                    NotificationManagerClass.DisplayWarningNotification("Splints Can Only Fix Fractures On Limbs", EFT.Communications.ENotificationDurationType.Long);

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
                        if (medType == "vas") 
                        {
                            return true;
                        }

                        if (Plugin.EnableLogging.Value)
                        {
                            Logger.LogWarning("After all checks, body part is still common, canceling heal");
                        }

                        NotificationManagerClass.DisplayWarningNotification("No Suitable Bodypart Was Found For Healing, Make Sure Gear Isn't Covering The Wound And That You Have The Right Medical Item", EFT.Communications.ENotificationDurationType.Long);
                        
                         return false;
                    }
                }

                //determine if any effects should be applied based on what is being healed
                if (bodyPart != EBodyPart.Common)
                {
                    handleHealthEffects(medType, meds, bodyPart, __instance, hBleedHealType, canHealHBleed, canHealLBleed, canHealFract);
                }
            }

            return true;
        }
    }

/*    //THIS IS DOG SHIT, IT WILL AFFECT BOTS, UNUSED
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
                float num = 1f - player.Skills.HealthHydration;
                __result = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyDamage * num * PlayerProperties.HealthResourceRateFactor / GetDecayRate(player);
                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("modified energy decay = " + __result);
                    Logger.LogWarning("original energy decay = " + Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyDamage * num / GetDecayRate(player));
                }
                return false;
            }
            return true;
 
        }
    }

    //THIS IS DOG SHIT, IT WILL AFFECT BOTS, UNUSED
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
                float num = 1f - player.Skills.HealthHydration;
                __result = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.HydrationDamage * num * PlayerProperties.HealthResourceRateFactor / GetDecayRate(player);
                return false;
            }
            return true;
        }
    }*/
}
